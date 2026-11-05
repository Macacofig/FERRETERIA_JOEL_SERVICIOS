using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Perfil de la cuenta autenticada. El front no consulta MySQL: pide la
    /// proyección pública a ServicioUsuarios (<c>GET /api/usuarios/me</c>) y la
    /// muestra junto al acceso al cambio de contraseña.
    /// </summary>
    [Authorize]
    public class PerfilModel : PageModel
    {
        private readonly UsuariosApiCliente _api;
        private readonly ILogger<PerfilModel> _logger;

        public PerfilModel(
            UsuariosApiCliente api,
            ILogger<PerfilModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public UsuarioApi? Usuario { get; set; }

        public string? MensajeError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActiveMenu"] = "Perfil";

            string? token = HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);

            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                Usuario = await _api.MeAsync(token);

                if (Usuario is null)
                {
                    return RedirectToPage("/Login");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el perfil del usuario.");
                MensajeError =
                    "No se pudo cargar la información de su cuenta. Inténtelo nuevamente.";
            }

            return Page();
        }
    }
}
