using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Helpers;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Comprobante/recibo de una venta. El identificador viaja cifrado en la
    /// ruta (token) para no exponer el id; el descifrado está delegado en
    /// <see cref="FERRETERIA__Joel.Helpers.UrlProtector"/>. Los datos y el PDF
    /// se piden a ServicioVentas con el JWT de la sesión.
    /// </summary>
    [Authorize]
    public class VentaComprobanteModel : PageModel
    {
        private readonly VentasApiCliente _api;
        private readonly ILogger<VentaComprobanteModel> _logger;

        public VentaComprobanteModel(
            VentasApiCliente api,
            ILogger<VentaComprobanteModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public string Token { get; private set; } = string.Empty;

        public ComprobanteVentaApi? Comprobante { get; private set; }

        public bool NoEncontrado { get; private set; }

        public async Task OnGetAsync(string token)
        {
            await CargarAsync(token);
        }

        /// <summary>
        /// Descarga el comprobante como PDF (?handler=Pdf). Reutiliza la misma
        /// resolución del token y el mismo endpoint que la vista en pantalla.
        /// </summary>
        public async Task<IActionResult> OnGetPdfAsync(string token)
        {
            if (!TryObtenerId(token, out int idVenta))
            {
                return NotFound();
            }

            try
            {
                byte[]? pdf = await _api.ObtenerComprobantePdfAsync(
                    ObtenerToken(),
                    idVenta);

                if (pdf is null)
                {
                    return NotFound();
                }

                return File(pdf, "application/pdf");
            }
            catch (VentasApiException ex) when (ex.Estado == 404)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo generar el PDF de la venta {IdVenta}", idVenta);
                return StatusCode(503);
            }
        }

        private async Task CargarAsync(string token)
        {
            Token = token;

            if (!TryObtenerId(token, out int idVenta))
            {
                NoEncontrado = true;
                return;
            }

            try
            {
                Comprobante = await _api.ObtenerComprobanteAsync(
                    ObtenerToken(),
                    idVenta);
            }
            catch (VentasApiException ex)
            {
                _logger.LogWarning(ex, "No se pudo cargar el comprobante de la venta {IdVenta}", idVenta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo cargar el comprobante de la venta {IdVenta}", idVenta);
            }

            if (Comprobante is null)
            {
                NoEncontrado = true;
            }
        }

        private static bool TryObtenerId(string token, out int idVenta)
        {
            idVenta = 0;

            string? texto = UrlProtector.Descifrar(token);

            return texto is not null
                && int.TryParse(texto, out idVenta)
                && idVenta > 0;
        }

        public static string ImprimirEstado(string estado)
        {
            return estado == EstadoVenta.Registrada
                ? "Registrada"
                : "Anulada";
        }

        private string? ObtenerToken()
        {
            return HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);
        }
    }
}
