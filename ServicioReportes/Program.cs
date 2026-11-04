using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// El .env de la raíz del repositorio aporta la clave de firma del JWT y la
// cadena de MySQL. Nunca se versiona; en Docker las variables llegan por el
// entorno del contenedor.
CargarVariablesEntorno(builder.Environment.ContentRootPath);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Authentication - La UI envía un token Bearer en cada request.
// Este servicio solo valida el token; no gestiona login ni sesiones.
// Se toma el primer valor realmente informado: appsettings deja "Jwt:Key"
// como cadena vacía y un '??' taparía la alternativa Jwt__Clave.
string? jwtKey = PrimeraNoVacia(
    builder.Configuration["Jwt:Key"],
    builder.Configuration["Jwt:Clave"],
    Environment.GetEnvironmentVariable("Jwt__Key"),
    Environment.GetEnvironmentVariable("Jwt__Clave"));

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Falta la clave 'Jwt:Key'. Defina la variable de entorno Jwt__Key.");
}

string jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? Environment.GetEnvironmentVariable("Jwt__Issuer")
    ?? "FerreteriaJoel";

string jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("Jwt__Audience")
    ?? "FerreteriaJoel";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "ServicioReportes v1"
        );
    });
}

// Solo se redirige a HTTPS si el servicio está configurado con un endpoint
// HTTPS. En Docker la API escucha en http://+:8080 y una redirección dejaría
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Sonda de salud usada por Docker Compose. Es pública a propósito.
app.MapGet("/health", () => Results.Ok(new
{
    servicio = "ServicioReportes",
    estado = "saludable",
    fecha = DateTime.UtcNow
}));

app.Run();

// Lee clave=valor del .env y define cada variable si aún no existe.
static void CargarVariablesEntorno(string directorio)
{
    string? ruta = BuscarArchivoEnv(directorio);

    if (ruta is null)
    {
        return;
    }

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
}

static string? BuscarArchivoEnv(string directorio)
{
    string? actual = Path.GetFullPath(directorio);

    for (int nivel = 0; nivel < 5 && actual is not null; nivel++)
    {
        string ruta = Path.Combine(actual, ".env");

        if (File.Exists(ruta))
        {
            return ruta;
        }

        DirectoryInfo? padre = Directory.GetParent(actual);
        actual = padre?.FullName;
    }

    return null;
}

// Devuelve el primer valor no nulo ni vacío (las claves vacías de
// appsettings no deben tapar las variables de entorno).
static string PrimeraNoVacia(params string?[] valores)
{
    return valores.FirstOrDefault(
            valor => !string.IsNullOrWhiteSpace(valor))
        ?? string.Empty;
}
