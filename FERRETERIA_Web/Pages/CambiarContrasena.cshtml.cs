using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Cambio de contraseña del empleado autenticado. El front no conoce el
    /// hash ni la política: envía las tres entradas a ServicioUsuarios y muestra
    /// los errores por campo que devuelve el servicio.
    /// </summary>
    [Authorize]
    public class CambiarContrasenaModel : PageModel
    {
        private readonly UsuariosApiCliente _api;
        private readonly ILogger<CambiarContrasenaModel> _logger;

        [BindProperty]
        public CambioContrasenaApi Solicitud { get; set; } = new();

        public string? Mensaje { get; set; }

        public bool Exitoso { get; set; }

        public Dictionary<string, string> ErroresCampo { get; set; } = new();

        public CambiarContrasenaModel(
            UsuariosApiCliente api,
            ILogger<CambiarContrasenaModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public void OnGet()
        {
            ViewData["ActiveMenu"] = "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["ActiveMenu"] = "";

            string? token = HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);

            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                RespuestaApi<object> respuesta =
                    await _api.CambiarContrasenaAsync(token, Solicitud);

                if (!respuesta.Exitoso)
                {
                    Mensaje = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                        ? "No se pudo cambiar la contraseña."
                        : respuesta.Mensaje;
                    ErroresCampo = respuesta.ErroresCampo;
                    return Page();
                }

                // El servicio revoca los tokens previos: la sesión actual ya no
                // sirve. Se cierra ordenadamente y se pide un login nuevo.
                HttpContext.Session.Remove(UsuariosApiCliente.ClaveSesionJwt);

                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                TempData["Mensaje"] =
                    "Contraseña actualizada. Inicie sesión con su nueva contraseña.";

                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar la contraseña.");
                Mensaje =
                    "No se pudo conectar con el servicio de usuarios. Inténtelo nuevamente.";
                return Page();
            }
        }
    }
}
