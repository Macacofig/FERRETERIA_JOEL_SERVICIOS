using System.Security.Claims;
using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Inicio de sesión del personal. El front no valida contraseñas ni
    /// consulta MySQL: envía las credenciales a ServicioUsuarios, recibe el JWT
    /// y guarda el token en la sesión de servidor. La cookie de autenticación
    /// solo contiene claims de presentación.
    /// </summary>
    public class LoginModel : PageModel
    {
        private readonly UsuariosApiCliente _api;
        private readonly ILogger<LoginModel> _logger;

        [BindProperty]
        public InicioSesionSolicitud Solicitud { get; set; } = new();

        public string? Mensaje { get; set; }

        public LoginModel(
            UsuariosApiCliente api,
            ILogger<LoginModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            ViewData["ActiveMenu"] = "Login";

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ViewData["ActiveMenu"] = "Login";

            string identificador =
                Solicitud.CorreoElectronico?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(identificador)
                || string.IsNullOrWhiteSpace(Solicitud.Contrasena))
            {
                Mensaje = "Ingrese su correo o usuario y su contraseña.";
                return Page();
            }

            RespuestaApi<LoginApiRespuesta> respuesta;

            try
            {
                respuesta = await _api.LoginAsync(
                    identificador,
                    Solicitud.Contrasena);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo contactar a ServicioUsuarios.");
                Mensaje =
                    "No se pudo conectar con el servicio de usuarios. Inténtelo nuevamente.";
                return Page();
            }

            if (!respuesta.Exitoso
                || respuesta.Datos is null
                || string.IsNullOrWhiteSpace(respuesta.Datos.Token))
            {
                Mensaje = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "Correo o contraseña incorrectos."
                    : respuesta.Mensaje;
                return Page();
            }

            LoginApiRespuesta sesion = respuesta.Datos;
            UsuarioApi usuario = sesion.Usuario;

            List<Claim> claims = new()
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.IdEmpleado.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(
                    ClaimTypes.Email,
                    usuario.CorreoElectronico ?? string.Empty),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("NombreCompleto", usuario.NombreCompleto),
                new Claim("NombreUsuario", usuario.NombreUsuario ?? string.Empty)
            };

            ClaimsIdentity identidad = new(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidad));

            // El JWT queda en la sesión de servidor (Redis): el navegador solo
            // recibe la cookie de sesión HttpOnly, nunca el token.
            HttpContext.Session.SetString(
                UsuariosApiCliente.ClaveSesionJwt,
                sesion.Token);

            TempData["Mensaje"] = $"Bienvenido, {usuario.NombreCompleto}.";

            if (!string.IsNullOrWhiteSpace(returnUrl)
                && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToPage("/Index");
        }

    }
}
