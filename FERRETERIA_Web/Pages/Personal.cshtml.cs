using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Gestión del personal. El listado y las operaciones de ciclo de vida se
    /// piden a ServicioUsuarios; la página no consulta MySQL ni modifica
    /// estados directamente.
    /// </summary>
    [Authorize(Roles = RolesSistema.Administrador)]
    public class PersonalModel : PageModel
    {
        private readonly UsuariosApiCliente _api;
        private readonly ILogger<PersonalModel> _logger;

        public PersonalModel(
            UsuariosApiCliente api,
            ILogger<PersonalModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        /// <summary>Personal con la proyección pública que expone la API.</summary>
        public List<UsuarioApi> Personal { get; set; } = new();

        public async Task OnGetAsync()
        {
            ViewData["ActiveMenu"] = "Registro";
            await CargarAsync();
        }

        /// <summary>Baja lógica de la cuenta.</summary>
        public async Task<IActionResult> OnPostBajaAsync(int idEmpleado)
        {
            await EjecutarAsync(
                () => _api.DarDeBajaAsync(ObtenerToken(), idEmpleado),
                "No se pudo completar la baja. Inténtalo nuevamente.");
            return RedirectToPage();
        }

        /// <summary>Reactivación con reenvío del enlace de activación.</summary>
        public async Task<IActionResult> OnPostReactivarAsync(int idEmpleado)
        {
            await EjecutarAsync(
                () => _api.ReactivarAsync(ObtenerToken(), idEmpleado),
                "No se pudo completar la reactivación. Inténtalo nuevamente.");
            return RedirectToPage();
        }

        /// <summary>Reenvío del enlace de activación de una cuenta pendiente.</summary>
        public async Task<IActionResult> OnPostReenviarAsync(int idEmpleado)
        {
            await EjecutarAsync(
                () => _api.ReenviarActivacionAsync(ObtenerToken(), idEmpleado),
                "No se pudo reenviar el enlace. Inténtalo nuevamente.");
            return RedirectToPage();
        }

        private async Task EjecutarAsync(
            Func<Task<RespuestaApi<object>>> operacion,
            string mensajeError)
        {
            try
            {
                RespuestaApi<object> resultado = await operacion();

                if (resultado.Exitoso)
                {
                    TempData["Mensaje"] = resultado.Mensaje;
                }
                else
                {
                    TempData["MensajeError"] = resultado.Mensaje;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al operar sobre el personal.");
                TempData["MensajeError"] = mensajeError;
            }
        }

        private async Task CargarAsync()
        {
            try
            {
                RespuestaApi<List<UsuarioApi>> respuesta =
                    await _api.ListarAsync(ObtenerToken());

                if (respuesta.Exitoso && respuesta.Datos is not null)
                {
                    Personal = respuesta.Datos;
                }
                else
                {
                    TempData["MensajeError"] = string.IsNullOrWhiteSpace(
                        respuesta.Mensaje)
                        ? "No se pudo cargar el listado del personal."
                        : respuesta.Mensaje;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar el personal.");
                TempData["MensajeError"] =
                    "No se pudo cargar el listado del personal.";
            }
        }

        private string? ObtenerToken()
        {
            return HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);
        }
    }
}
