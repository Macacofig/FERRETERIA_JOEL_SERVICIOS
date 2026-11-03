using ServicioVentas.Aplicacion.Servicios;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura;
using ServicioVentas.Infraestructura.Conexion;
using ServicioVentas.Infraestructura.Repositorios;
using ServicioVentas.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// JWT Authentication - La UI envía un token Bearer en cada request.
// Este servicio solo valida el token; no gestiona login ni sesiones.
string? jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("Jwt__Key");

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

// Repositorios
builder.Services.AddScoped<MySqlVentaRepository>();
builder.Services.AddScoped<MySqlDetalleVentaRepository>();
builder.Services.AddScoped<MySqlClienteRepository>();
builder.Services.AddScoped<MySqlProductoRepository>();
builder.Services.AddScoped<MySqlInventarioRepository>();
builder.Services.AddScoped<MySqlCategoriaRepository>();
builder.Services.AddScoped<MySqlEmpleadoRepository>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

// Sonda de salud usada por Docker Compose.
app.MapGet("/health", () => Results.Ok(new
{
    servicio = "ServicioVentas",
    estado = "saludable",
    fecha = DateTime.UtcNow
}));

app.Run();
