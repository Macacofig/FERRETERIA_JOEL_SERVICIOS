using ServicioUsuarios.Application.Ports;
using ServicioUsuarios.Adapters.Out.MySql;
using ServicioUsuarios.Adapters.Out.Security;
using ServicioUsuarios.Application.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ServicioUsuarios.Application.Services;
using ServicioUsuarios.Adapters.Out.Smtp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<MySqlOptions>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IMySqlConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddScoped<IUsuarioRepository, MySqlUsuarioRepository>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IPasswordPolicy, PasswordPolicy>();
builder.Services.AddScoped<IUsernameGenerator, UsernameGenerator>();
string activationUrlBase = builder.Configuration["Activation:UrlBase"] ?? string.Empty;
builder.Services.AddScoped<UsuariosUseCase>(provider => new UsuariosUseCase(
    provider.GetRequiredService<IUsuarioRepository>(),
    provider.GetRequiredService<IUsernameGenerator>(),
    provider.GetRequiredService<IPasswordHasher>(),
    provider.GetRequiredService<IPasswordPolicy>(),
    provider.GetRequiredService<IEmailSender>(),
    activationUrlBase));

JwtOptions jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("La sección de configuración Jwt es requerida.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
