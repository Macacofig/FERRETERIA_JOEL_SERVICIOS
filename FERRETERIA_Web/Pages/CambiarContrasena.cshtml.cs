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
                RespuestaApi<CambioContrasenaApiRespuesta> respuesta =
                    await _api.CambiarContrasenaAsync(token, Solicitud);

                if (!respuesta.Exitoso)
                {
                    Mensaje = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                        ? "No se pudo cambiar la contraseña."
                        : respuesta.Mensaje;
                    ErroresCampo = respuesta.ErroresCampo;
                    return Page();
                }

                string? nuevoToken = respuesta.Datos?.Token;

                if (string.IsNullOrWhiteSpace(nuevoToken))
                {
                    // El servicio no pudo renovar la sesión: se cierra por
                    // seguridad y se pide un inicio de sesión nuevo.
                    HttpContext.Session.Remove(UsuariosApiCliente.ClaveSesionJwt);

                    await HttpContext.SignOutAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme);

                    TempData["Mensaje"] =
                        "Contraseña actualizada. Inicie sesión nuevamente.";

                    return RedirectToPage("/Login");
                }

                // El servicio revocó los tokens anteriores y devolvió uno
                // nuevo con la generación vigente: la sesión actual continúa
                // activa sin pasar por el login.
                HttpContext.Session.SetString(
                    UsuariosApiCliente.ClaveSesionJwt,
                    nuevoToken);

                TempData["Mensaje"] = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                    ? "Contraseña actualizada correctamente. Su sesión continúa activa."
                    : respuesta.Mensaje;

                return RedirectToPage("/Perfil");
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
