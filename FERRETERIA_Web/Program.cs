using Microsoft.AspNetCore.Authentication.Cookies;
using FERRETERIA_Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddTransient<ServicioUsuariosBearerHandler>();
builder.Services.AddHttpClient<IServicioUsuariosClient, ServicioUsuariosClient>((provider, client) =>
{
    string baseUrl = provider.GetRequiredService<IConfiguration>()["ServicioUsuarios:BaseUrl"]
        ?? throw new InvalidOperationException("La URL de ServicioUsuarios no está configurada.");
    client.BaseAddress = new Uri(baseUrl);
})
.AddHttpMessageHandler<ServicioUsuariosBearerHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
