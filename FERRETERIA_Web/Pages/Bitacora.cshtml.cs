using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Consulta de la bitácora de auditoría. El front solo muestra los eventos
    /// que expone ServicioUsuarios: responsable, operación, entidad, registro
    /// afectado, fecha y resultado. La tabla no contiene credenciales.
    /// </summary>
    [Authorize(Roles = RolesSistema.Administrador)]
    public class BitacoraModel : PageModel
    {
        private readonly UsuariosApiCliente _api;
        private readonly ILogger<BitacoraModel> _logger;

        public BitacoraModel(
            UsuariosApiCliente api,
            ILogger<BitacoraModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public List<BitacoraApi> Registros { get; set; } = new();

        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            ViewData["ActiveMenu"] = "Bitacora";

            try
            {
                RespuestaApi<List<BitacoraApi>> respuesta =
                    await _api.ListarBitacoraAsync(ObtenerToken(), 200);

                if (respuesta.Exitoso && respuesta.Datos is not null)
                {
                    Registros = respuesta.Datos;
                }
                else
                {
                    MensajeError = string.IsNullOrWhiteSpace(respuesta.Mensaje)
                        ? "No se pudo cargar la bitácora."
                        : respuesta.Mensaje;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar la bitácora.");
                MensajeError = "No se pudo cargar la bitácora.";
            }
        }

        private string? ObtenerToken()
        {
            return HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);
        }
    }
}
