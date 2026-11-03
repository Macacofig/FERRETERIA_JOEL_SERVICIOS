using System.Security.Claims;
using FERRETERIA__Joel.Helpers;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

CargarVariablesEntorno(builder.Environment.ContentRootPath);
builder.Configuration.AddEnvironmentVariables();

string claveUrl = builder.Configuration["UrlProteccion:Clave"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(claveUrl))
{
    throw new InvalidOperationException(
        "Falta 'UrlProteccion:Clave'. Defina UrlProteccion__Clave en el .env.");
}

UrlProtector.Inicializar(claveUrl);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "FerreteriaJoel.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

string apiUrl = builder.Configuration["Servicios:UsuariosApiUrl"]
    ?? builder.Configuration["Servicios__UsuariosApiUrl"]
    ?? "http://localhost:5200";

builder.Services.AddHttpClient<UsuariosApiCliente>(client =>
{
    client.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.Cookie.Name = "FerreteriaJoel.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            string? jwt = context.HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);

            if (string.IsNullOrWhiteSpace(jwt))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
                return;
            }

            UsuariosApiCliente api = context.HttpContext.RequestServices
                .GetRequiredService<UsuariosApiCliente>();

            if (await api.MeAsync(jwt) is null)
            {
                context.HttpContext.Session.Remove(
                    UsuariosApiCliente.ClaveSesionJwt);
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

static void CargarVariablesEntorno(string directorio)
{
    string? actual = Path.GetFullPath(directorio);

    for (int nivel = 0; nivel < 5 && actual is not null; nivel++)
    {
        string ruta = Path.Combine(actual, ".env");

        if (File.Exists(ruta))
        {
            foreach (string linea in File.ReadAllLines(ruta))
            {
                string texto = linea.Trim();

                if (texto.Length == 0 || texto.StartsWith('#'))
                {
                    continue;
                }

                int separador = texto.IndexOf('=');

                if (separador <= 0)
                {
                    continue;
                }

                string clave = texto[..separador].Trim();
                string valor = texto[(separador + 1)..].Trim().Trim('"');

                if (Environment.GetEnvironmentVariable(clave) is null)
                {
                    Environment.SetEnvironmentVariable(clave, valor);
                }
            }

            return;
        }

        actual = Directory.GetParent(actual)?.FullName;
    }
}
