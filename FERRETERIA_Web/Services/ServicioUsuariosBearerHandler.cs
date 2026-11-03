using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FERRETERIA_Web.Services;

public sealed class ServicioUsuariosBearerHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServicioUsuariosBearerHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpContext? context = _httpContextAccessor.HttpContext;
        if (context is not null)
        {
            AuthenticateResult result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            string? token = result.Properties?.GetTokenValue("access_token");
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
