using System.Security.Claims;
using FERRETERIA__Joel.Helpers;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// El .env de la raíz aporta la clave del JWT (y del protector de URLs) en
// desarrollo local. En Docker las variables llegan por el entorno.
Dictionary<string, string?> configuracionLocal =
    CargarVariablesEntorno(builder.Environment.ContentRootPath);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddInMemoryCollection(
    configuracionLocal);

string claveUrl = PrimeraNoVacia(
    configuracionLocal.GetValueOrDefault("UrlProteccion:Clave"),
    configuracionLocal.GetValueOrDefault("Jwt:Key"),
    Environment.GetEnvironmentVariable("UrlProteccion__Clave"),
    Environment.GetEnvironmentVariable("Jwt__Key"),
    builder.Configuration["UrlProteccion:Clave"],
    builder.Configuration["Jwt:Key"]);

if (string.IsNullOrWhiteSpace(claveUrl))
{
    throw new InvalidOperationException(
        "Falta 'Jwt:Key' o 'UrlProteccion:Clave' en el .env.");
}

// Misma clave en Web y ServicioUsuarios: los enlaces de comprobante cifrados
// por la web se descifran en el servicio con AES-GCM.
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

string ventasApiUrl = builder.Configuration["Servicios:VentasApiUrl"]
    ?? builder.Configuration["Servicios__VentasApiUrl"]
    ?? "http://localhost:5071";

builder.Services.AddHttpClient<VentasApiCliente>(client =>
{
    client.BaseAddress = new Uri(ventasApiUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

string reportesApiUrl = builder.Configuration["Servicios:ReportesApiUrl"]
    ?? builder.Configuration["Servicios__ReportesApiUrl"]
?? "http://localhost:5073";

// La generación de PDF/Excel puede tardar más que una consulta JSON.
builder.Services.AddHttpClient<ReportesApiCliente>(client =>
{
    client.BaseAddress = new Uri(reportesApiUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(90);
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

// Solo se redirige a HTTPS si la aplicación está configurada con un endpoint
// HTTPS. En Docker la web escucha en http://+:8080 y una redirección dejaría
// inaccesible el puerto (y rompería el healthcheck del contenedor).
bool sirveHttps =
    app.Urls.Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    || (builder.Configuration["Urls"] ?? string.Empty)
        .Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Any(url => url.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase));

if (sirveHttps)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

static Dictionary<string, string?> CargarVariablesEntorno(string directorio)
{
    Dictionary<string, string?> variables = new(
        StringComparer.OrdinalIgnoreCase);

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

                variables[clave.Replace("__", ":", StringComparison.Ordinal)] = valor;

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(clave)))
                {
                    Environment.SetEnvironmentVariable(clave, valor);
                }
            }

            return variables;
        }

        actual = Directory.GetParent(actual)?.FullName;
    }

    return variables;
}

static string PrimeraNoVacia(params string?[] valores)
{
    return valores.FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor))
        ?? string.Empty;
}
