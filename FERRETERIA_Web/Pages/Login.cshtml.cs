using System.Security.Claims;
using FERRETERIA_Web.Models;
using FERRETERIA_Web.Services;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA_Web.Pages;

public sealed class LoginModel : PageModel
{
    private readonly IServicioUsuariosClient _servicioUsuarios;
    private readonly IConfiguration _configuration;

    public LoginModel(IServicioUsuariosClient servicioUsuarios, IConfiguration configuration)
    {
        _servicioUsuarios = servicioUsuarios;
        _configuration = configuration;
    }

    [BindProperty]
    public LoginRequest Solicitud { get; set; } = new(string.Empty, string.Empty);

    public string? Mensaje { get; private set; }

    public IActionResult OnGet()
    {
        return User.Identity?.IsAuthenticated == true
            ? RedirectToPage("/Index")
            : Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        var resultado = await _servicioUsuarios.LoginAsync(Solicitud, cancellationToken);
        if (!resultado.Exitoso)
        {
            Mensaje = resultado.Error;
            return Page();
        }

        string claveJwt = _configuration["Jwt:Key"] ?? string.Empty;
        string emisorEsperado = _configuration["Jwt:Issuer"] ?? string.Empty;
        string audienciaEsperada = _configuration["Jwt:Audience"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(claveJwt))
        {
            Mensaje = "La autenticación no está configurada correctamente.";
            return Page();
        }

        try
        {
            IDictionary<string, object> payload = JwtBuilder.Create()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(claveJwt)
                .MustVerifySignature()
                .Decode<IDictionary<string, object>>(resultado.Token);

            string id = ObtenerClaim(payload, "sub") ?? ObtenerClaim(payload, ClaimTypes.NameIdentifier) ?? string.Empty;
            string nombreUsuario = ObtenerClaim(payload, "unique_name")
                ?? ObtenerClaim(payload, ClaimTypes.Name)
                ?? string.Empty;
            string rol = ObtenerClaim(payload, "role")
                ?? ObtenerClaim(payload, ClaimTypes.Role)
                ?? string.Empty;

            string emisor = ObtenerClaim(payload, "iss") ?? string.Empty;
            string audiencia = ObtenerClaim(payload, "aud") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(emisorEsperado) && !emisor.Equals(emisorEsperado, StringComparison.Ordinal))
                throw new InvalidOperationException("Emisor JWT inválido.");
            if (!string.IsNullOrWhiteSpace(audienciaEsperada) && !audiencia.Equals(audienciaEsperada, StringComparison.Ordinal))
                throw new InvalidOperationException("Audiencia JWT inválida.");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(nombreUsuario) ||
                (rol != "Administrador" && rol != "EmpleadoVenta"))
                throw new InvalidOperationException("Claims JWT incompletos.");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, id),
                new(ClaimTypes.Name, nombreUsuario),
                new(ClaimTypes.Role, rol)
            };
            var identidad = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name,
                ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identidad);
            var propiedades = new AuthenticationProperties
            {
                IsPersistent = false,
                RedirectUri = returnUrl
            };
            propiedades.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = resultado.Token }
            });

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                propiedades);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToPage("/Index");
        }
        catch
        {
            Mensaje = "No fue posible validar la sesión iniciada.";
            return Page();
        }
    }

    private static string? ObtenerClaim(IDictionary<string, object> payload, string nombre)
    {
        if (!payload.TryGetValue(nombre, out object? valor))
        {
            foreach (var par in payload)
            {
                if (par.Key.EndsWith("/" + nombre, StringComparison.OrdinalIgnoreCase))
                {
                    valor = par.Value;
                    break;
                }
            }
        }
        return valor?.ToString();
    }
}
