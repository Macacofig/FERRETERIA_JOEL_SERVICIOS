using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Cierre de sesión. Revoca el JWT en ServicioUsuarios, elimina la sesión
    /// de Redis y borra la cookie de autenticación del front.
    /// </summary>
    public class LogoutModel : PageModel
    {
        private readonly UsuariosApiCliente _api;
        private readonly ILogger<LogoutModel> _logger;
        public LogoutModel(
            UsuariosApiCliente api,
            ILogger<LogoutModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string? token = HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);

            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    await _api.LogoutAsync(token);
                }
                catch (Exception ex)
                {
                    // La cookie local se elimina igual: el usuario no queda
                    // atrapado en una sesión si la API está caída.
                    _logger.LogError(
                        ex,
                        "No se pudo revocar el token en ServicioUsuarios.");
                }
            }

            HttpContext.Session.Remove(UsuariosApiCliente.ClaveSesionJwt);

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Mensaje"] = "Sesión cerrada correctamente.";

            return RedirectToPage("/Index");
        }
    }
}
