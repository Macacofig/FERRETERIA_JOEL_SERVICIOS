using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Listado de ventas con modal de detalle (solo lectura) y anulación
    /// lógica con motivo. Los datos y la anulación se delegan en
    /// ServicioVentas; la página no consulta MySQL. El administrador solo
    /// consulta; el EmpleadoVenta además puede anular.
    /// </summary>
    [Authorize(Roles = RolesSistema.Personal)]
    public class VentasModel : PageModel
    {
        private readonly VentasApiCliente _api;
        private readonly ILogger<VentasModel> _logger;

        public VentasModel(VentasApiCliente api, ILogger<VentasModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public List<VentaListadoApi> Ventas { get; private set; } = new();

        [BindProperty]
        public int IdVenta { get; set; }

        [BindProperty]
        public string? MotivoAnulacion { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                Ventas = await _api.ListarVentasAsync(ObtenerToken());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo cargar el historial de ventas");
                TempData["MensajeError"] =
                    "No se pudo cargar el historial. Revise la conexión con el servicio de ventas.";
                Ventas = new List<VentaListadoApi>();
            }
        }

        public async Task<IActionResult> OnGetDetalleAsync(int idVenta)
        {
            DetalleVentaApi? detalle;
            try
            {
                detalle = await _api.ObtenerDetalleAsync(
                    ObtenerToken(),
                    idVenta);
            }
            catch (VentasApiException ex) when (ex.Estado == 404)
            {
                return new JsonResult(null);
            }
            catch (VentasApiException ex)
            {
                _logger.LogWarning(ex, "No se pudo cargar el detalle de venta {IdVenta}", idVenta);
                return StatusCode(ex.Estado, new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo cargar el detalle de venta {IdVenta}", idVenta);
                return StatusCode(503, new
                {
                    mensaje = "No se pudo consultar el detalle de la venta."
                });
            }

            if (detalle is null)
            {
                return new JsonResult(null);
            }

            return new JsonResult(detalle);
        }

        public async Task<IActionResult> OnPostAnularAsync(
            int idVenta,
            string motivoAnulacion)
        {
            if (!User.IsInRole(RolesSistema.EmpleadoVenta))
            {
                return Forbid();
            }

            if (idVenta <= 0)
            {
                TempData["MensajeError"] = "No se recibió una venta válida para anular.";
                return RedirectToPage();
            }

            try
            {
                ResultadoVentaApi resultado = await _api.AnularVentaAsync(
                    ObtenerToken(),
                    idVenta,
                    motivoAnulacion?.Trim() ?? string.Empty);

                if (!resultado.Exito)
                {
                    TempData["MensajeError"] = resultado.Mensaje;
                }
                else
                {
                    TempData["Mensaje"] =
                        $"Venta {resultado.CodigoVenta} anulada. El stock fue restaurado.";
                }
            }
            catch (VentasApiException ex)
            {
                _logger.LogWarning(ex, "No se pudo anular la venta {IdVenta}", idVenta);
                TempData["MensajeError"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo anular la venta {IdVenta}", idVenta);
                TempData["MensajeError"] =
                    "No se pudo anular la venta. Revise la conexión con el servicio de ventas.";
            }

            return RedirectToPage();
        }

        private string? ObtenerToken()
        {
            return HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);
        }
    }
}
