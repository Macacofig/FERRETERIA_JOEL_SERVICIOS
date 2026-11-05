using System.Security.Claims;
using System.Text;
using FERRETERIA__Joel.Aplicacion.Servicios;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Utilidades;
using FERRETERIA__Joel.Infraestructura.Conexion;
using FERRETERIA__Joel.Infraestructura.Factories;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// El .env de la raíz aporta MySQL, JWT, protección de enlaces y SMTP.
// Nunca se versiona.
Dictionary<string, string?> configuracionLocal =
    CargarVariablesEntorno(builder.Environment.ContentRootPath);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddInMemoryCollection(
    configuracionLocal);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(ConfiguracionSwaggerBearer);

// ---------------------------------------------------------------------------
// Persistencia: se reutiliza exactamente el mismo esquema MySQL del monolito.
// La API no tiene tablas propias ni un almacén de usuarios paralelo.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();

// Repositorios: patrón Factory Method. Los servicios reciben creadores y
// piden el repositorio concreto a través del contrato del dominio; nunca
// conocen las clases MySql*.
builder.Services.AddScoped<RepositoryCreator<IRepositoryEmpleado>, EmpleadoRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<IRepositoryBitacora>, BitacoraRepositoryCreator>();

// ---------------------------------------------------------------------------
// Servicios de aplicación existentes (reutilizados, no duplicados).
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IHasheadorContrasena, Argon2HasheadorContrasena>();
builder.Services.AddSingleton<ICredencialesValidaciones, CredencialesValidaciones>();
builder.Services.AddScoped<IEnviadorCorreo, SmtpEnviadorCorreo>();
builder.Services.AddScoped<IConstructorEnlaceActivacion, ConstructorEnlaceActivacionApi>();
builder.Services.AddScoped<RegistroCredencialesValidador>();
builder.Services.AddScoped<ServicioRegistroEmpleado>();
builder.Services.AddScoped<ServicioActivacionCuenta>();
builder.Services.AddScoped<ServicioInicioSesion>();
builder.Services.AddScoped<ServicioPersonal>();
builder.Services.AddScoped<ServicioCambioContrasena>();
builder.Services.AddScoped<ServicioEdicionEmpleado>();
builder.Services.AddScoped<ServicioBitacora>();

// La identidad dentro de la API se resuelve desde el JWT validado.
builder.Services.AddScoped<IContextoUsuario, ContextoUsuarioJwt>();

builder.Services.AddSingleton<RevocacionTokens>();

builder.Services.AddSingleton<IRevocacionTokens>(sp =>
    sp.GetRequiredService<RevocacionTokens>());

builder.Services.AddSingleton<IEmisorToken, JwtEmisor>();

// ---------------------------------------------------------------------------
// JWT Bearer: única tecnología de autenticación de la API. Issuer, audience,
// expiración y firma se validan en cada petición.
// ---------------------------------------------------------------------------
// appsettings deja estas claves como cadena vacía; un '??' no saltaría al
// siguiente candidato y la aplicación arrancaría con clave vacía. Se toma el
// primer valor realmente informado (entorno/.env incluido).
string claveJwt = PrimeraNoVacia(
    builder.Configuration["Jwt:Key"],
    builder.Configuration["Jwt:Clave"],
    Environment.GetEnvironmentVariable("Jwt__Key"),
    Environment.GetEnvironmentVariable("Jwt__Clave"));

if (claveJwt.Length < 32)
{
    throw new InvalidOperationException(
        "Falta 'Jwt:Key' o tiene menos de 32 caracteres. Defina la variable "
        + "de entorno Jwt__Key (archivo .env de la raíz).");
}

// El token de activación se cifra con la misma clave/algoritmo que el front.
// Si no se define una clave propia, se reutiliza la del JWT para que ambos
// procesos descifren el mismo enlace.
string claveUrl = PrimeraNoVacia(
    builder.Configuration["UrlProteccion:Clave"],
    builder.Configuration["Jwt:Key"],
    builder.Configuration["Jwt:Clave"],
    Environment.GetEnvironmentVariable("UrlProteccion__Clave"),
    Environment.GetEnvironmentVariable("Jwt__Key"),
    Environment.GetEnvironmentVariable("Jwt__Clave"));

if (string.IsNullOrWhiteSpace(claveUrl))
{
    throw new InvalidOperationException(
        "Falta 'UrlProteccion:Clave' o 'Jwt:Key'. Defina UrlProteccion__Clave "
        + "o Jwt__Key en el .env.");
}

UrlProtectorApi.Inicializar(claveUrl);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // El emisor firma con ClaimTypes.* y el handler los serializa con los
        // nombres cortos estándar (nameid, role, unique_name...). El mapeo
        // entrante restaura esos tipos para que [Authorize(Roles = ...)] y
        // ClaimTypes.NameIdentifier funcionen. No afecta a 'jti', que conserva
        // su nombre corto.
        options.MapInboundClaims = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(claveJwt)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = contexto =>
            {
                IRevocacionTokens revocacion = contexto.HttpContext
                    .RequestServices.GetRequiredService<IRevocacionTokens>();

                string? jti = contexto.Principal?
                    .FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)
                    ?.Value;

                if (revocacion.EstaRevocado(jti))
                {
                    contexto.Fail("El token fue revocado (sesión cerrada).");
                }

                int idEmpleado = int.TryParse(
                    contexto.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    out int id) ? id : 0;

                int generacionToken = int.TryParse(
                    contexto.Principal?.FindFirst("gen")?.Value,
                    out int generacion) ? generacion : 0;

                if (!revocacion.EstaVigenteUsuario(
                        idEmpleado,
                        generacionToken))
                {
                    contexto.Fail("La sesión fue invalidada por un cambio de estado o de contraseña.");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Solo se redirige a HTTPS si el servicio está configurado con un endpoint
// HTTPS. En desarrollo local la API escucha en http://localhost:5200 y una
// redirección dejaría inaccesible el puerto.
bool sirveHttps =
    app.Urls.Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    || (builder.Configuration["Urls"] ?? string.Empty)
        .Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Any(url => url.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase));

if (sirveHttps)
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "ServicioUsuarios v1");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Sonda de salud usada por el front y por el orquestador.
app.MapGet("/health", () => Results.Ok(new
{
    servicio = "ServicioUsuarios",
    estado = "saludable",
    fecha = DateTime.UtcNow
}));

app.Run();

// Lee clave=valor del .env sin modificar el entorno del proceso.
static Dictionary<string, string?> CargarVariablesEntorno(string directorio)
{
    Dictionary<string, string?> variables = new(
        StringComparer.OrdinalIgnoreCase);

    string? ruta = BuscarArchivoEnv(directorio);

    if (ruta is null)
    {
        return variables;
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

        variables[clave.Replace("__", ":", StringComparison.Ordinal)] = valor;

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(clave)))
        {
            Environment.SetEnvironmentVariable(clave, valor);
        }
    }

    return variables;
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

static void ConfiguracionSwaggerBearer(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions opciones)
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ServicioUsuarios",
        Version = "v1",
        Description = "API de autenticación, empleados, bitácora y activación de cuentas."
    });

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese el JWT emitido por api/usuarios/login.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
}

// Devuelve el primer valor no nulo ni vacío. Evita que una clave vacía en
// appsettings.json tape el valor real que llega por variables de entorno.
static string PrimeraNoVacia(params string?[] valores)
{
    return valores.FirstOrDefault(
            valor => !string.IsNullOrWhiteSpace(valor))
        ?? string.Empty;
}
