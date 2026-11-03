using System.Security.Claims;
using System.Text;
using FERRETERIA__Joel.Aplicacion.Servicios;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Infraestructura.Conexion;
using FERRETERIA__Joel.Infraestructura.Repositorios;
using FERRETERIA__Joel.Infraestructura.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// El .env de la raÃ­z aporta MySQL, JWT, protecciÃ³n de enlaces y SMTP.
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
// La API no tiene tablas propias ni un almacÃ©n de usuarios paralelo.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddScoped<MySqlEmpleadoRepository>();
builder.Services.AddScoped<IRepositoryEmpleado>(sp =>
    sp.GetRequiredService<MySqlEmpleadoRepository>());
builder.Services.AddScoped<MySqlBitacoraRepository>();

// ---------------------------------------------------------------------------
// Servicios de aplicaciÃ³n existentes (reutilizados, no duplicados).
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IHasheadorContrasena, Argon2HasheadorContrasena>();
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

builder.Services.AddSingleton<JwtEmisor>();
builder.Services.AddSingleton<RevocacionTokens>();

// ---------------------------------------------------------------------------
// JWT Bearer: Ãºnica tecnologÃ­a de autenticaciÃ³n de la API. Issuer, audience,
// expiraciÃ³n y firma se validan en cada peticiÃ³n.
// ---------------------------------------------------------------------------
string claveJwt = configuracionLocal.GetValueOrDefault("Jwt:Key")
    ?? builder.Configuration["Jwt:Key"]
    ?? string.Empty;

if (claveJwt.Length < 32)
{
    throw new InvalidOperationException(
        "Falta 'Jwt:Key' o tiene menos de 32 caracteres. Defina la variable "
        + "de entorno Jwt__Key (archivo .env de la raÃ­z).");
}

// El token de activaciÃ³n se cifra con una clave independiente cuando se
// configura. Para el despliegue mÃ­nimo se deriva de Jwt:Key, de modo que el
// .env solo necesita las variables JWT compartidas por los servicios.
string? claveUrlConfigurada = builder.Configuration["UrlProteccion:Clave"];
string claveUrl = string.IsNullOrWhiteSpace(claveUrlConfigurada)
    ? claveJwt
    : claveUrlConfigurada;

UrlProtectorApi.Inicializar(claveUrl);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // El emisor firma con ClaimTypes.* y el handler los serializa con los
        // nombres cortos estÃ¡ndar (nameid, role, unique_name...). El mapeo
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
                RevocacionTokens revocacion = contexto.HttpContext
                    .RequestServices.GetRequiredService<RevocacionTokens>();

                string? jti = contexto.Principal?
                    .FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)
                    ?.Value;

                if (revocacion.EstaRevocado(jti))
                {
                    contexto.Fail("El token fue revocado (sesiÃ³n cerrada).");
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
                    contexto.Fail("La sesiÃ³n fue invalidada por un cambio de estado o de contraseÃ±a.");
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

// Solo se redirige a HTTPS si el servicio estÃ¡ configurado con un endpoint
// HTTPS. En desarrollo local la API escucha en http://localhost:5200 y una
// redirecciÃ³n dejarÃ­a inaccesible el puerto.
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

