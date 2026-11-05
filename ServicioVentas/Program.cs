using ServicioVentas.Aplicacion.Servicios;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Infraestructura;
using ServicioVentas.Infraestructura.Conexion;
using ServicioVentas.Infraestructura.Factories;
using ServicioVentas.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// El .env de la raíz del repositorio aporta la clave de firma del JWT y la
// cadena de MySQL. Nunca se versiona; en Docker las variables llegan por el
// entorno del contenedor.
CargarVariablesEntorno(builder.Environment.ContentRootPath);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ServicioVentas",
        Version = "v1",
        Description = "API de ventas, clientes, productos y anulación de ventas."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese el JWT emitido por ServicioUsuarios.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});

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

string? jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? Environment.GetEnvironmentVariable("Jwt__Issuer")
    ?? "ServicioVentas";

string? jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("Jwt__Audience")
    ?? "ServicioVentas";

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

// MySQL connection
builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddScoped<IConexionTransaccional, MySqlConexionTransaccional>();
builder.Services.AddScoped<IUnidadDeTrabajo, MySqlUnidadDeTrabajo>();

// Repositorios: patrón Factory Method. Las fachadas reciben creadores y
// piden el repositorio concreto a través del contrato del dominio; nunca
// conocen las clases MySql*.
builder.Services.AddScoped<RepositoryCreator<IVentaRepository>, VentaRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<IDetalleVentaRepository>, DetalleVentaRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<IClienteRepository>, ClienteRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<IProductoRepository>, ProductoRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<IInventarioRepository>, InventarioRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<ICategoriaRepository>, CategoriaRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<IEmpleadoRepository>, EmpleadoRepositoryCreator>();

// Servicios de aplicación (fachadas de ventas)
builder.Services.AddScoped<ServicioCliente>();
builder.Services.AddScoped<FachadaActualizacionStock>();
builder.Services.AddScoped<FachadaVenta>();
builder.Services.AddScoped<FachadaAnulacion>();

// Contexto de usuario (JWT)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IContextoUsuario, ContextoUsuarioJwt>();

// Generador de comprobante PDF
builder.Services.AddSingleton<IGeneradorComprobantePdf, QuestPdfGeneradorComprobante>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ServicioVentas v1");
});

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

// Sonda de salud usada por Docker Compose.
app.MapGet("/health", () => Results.Ok(new
{
    servicio = "ServicioVentas",
    estado = "saludable",
    fecha = DateTime.Now
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
