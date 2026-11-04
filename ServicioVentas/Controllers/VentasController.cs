using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.Servicios;
using ServicioVentas.Dominio.Constantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServicioVentas.Controllers
{
    /// <summary>
    /// API de Ventas. Expone los endpoints para registrar ventas, consultar
    /// el historial, ver el detalle, anular y obtener el comprobante.
    ///
    /// La UI debe enviar un JWT token en el header Authorization: Bearer <token>.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VentasController : ControllerBase
    {
        private readonly FachadaVenta _fachadaVenta;
        private readonly FachadaAnulacion _fachadaAnulacion;
        private readonly IGeneradorComprobantePdf _generadorPdf;
        private readonly ILogger<VentasController> _logger;

        public VentasController(
            FachadaVenta fachadaVenta,
            FachadaAnulacion fachadaAnulacion,
            IGeneradorComprobantePdf generadorPdf,
            ILogger<VentasController> logger)
        {
            _fachadaVenta = fachadaVenta;
            _fachadaAnulacion = fachadaAnulacion;
            _generadorPdf = generadorPdf;
            _logger = logger;
        }

        // ---------- Registro de venta ----------

        /// <summary>
        /// Registra una nueva venta. Solo rol EmpleadoVenta.
        /// </summary>
        [HttpPost("registrar")]
        [Authorize(Roles = RolesSistema.EmpleadoVenta)]
        public async Task<IActionResult> Registrar([FromBody] SolicitudVenta solicitud)
        {
            try
            {
                ResultadoVenta resultado = await _fachadaVenta.RegistrarVentaAsync(solicitud);

                if (!resultado.Exito)
                {
                    return BadRequest(new { exito = false, mensaje = resultado.Mensaje });
                }

                return Ok(new
                {
                    exito = true,
                    mensaje = resultado.Mensaje,
                    idVenta = resultado.IdVenta,
                    codigoVenta = resultado.CodigoVenta,
                    montoTotal = resultado.MontoTotal,
                    detalles = resultado.Detalles.Select(d => new
                    {
                        nombre = d.Nombre,
                        codigo = d.Codigo,
                        unidad = d.UnidadMedida,
                        cantidad = d.Cantidad,
                        precio = d.PrecioUnitario,
                        total = d.TotalProducto
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado al registrar la venta");
                return StatusCode(503, new { exito = false, mensaje = "No se pudo registrar la venta. Intente nuevamente." });
            }
        }

        // ---------- Historial de ventas ----------

        /// <summary>
        /// Lista todas las ventas (historial). Solo rol Personal (Administrador o EmpleadoVenta).
        /// </summary>
        [HttpGet("historial")]
        [Authorize(Roles = RolesSistema.Personal)]
        public async Task<IActionResult> Historial()
        {
            try
            {
                List<VentaListado> ventas = await _fachadaAnulacion.ListarVentasListadoAsync();
                return Ok(new { exito = true, ventas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo cargar el historial de ventas");
                return StatusCode(503, new { exito = false, mensaje = "No se pudo cargar el historial de ventas." });
            }
        }

        /// <summary>
        /// Obtiene el detalle de una venta por su ID. Solo rol Personal.
        /// </summary>
        [HttpGet("detalle/{idVenta}")]
        [Authorize(Roles = RolesSistema.Personal)]
        public async Task<IActionResult> Detalle(int idVenta)
        {
            try
            {
                ResultadoVenta? resultado = await _fachadaAnulacion.ObtenerVentaAsync(idVenta);

                if (resultado is null)
                {
                    return NotFound(new { exito = false, mensaje = "Venta no encontrada." });
                }

                return Ok(new
                {
                    exito = true,
                    codigoVenta = resultado.CodigoVenta,
                    estadoVenta = resultado.EstadoVenta,
                    metodoPago = resultado.MetodoPago,
                    estadoPago = resultado.EstadoPago,
                    fecha = resultado.FechaRegistro,
                    total = resultado.MontoTotal,
                    motivoAnulacion = resultado.MotivoAnulacion,
                    lineas = resultado.Detalles.Select(d => new
                    {
                        nombre = d.Nombre,
                        codigo = d.Codigo,
                        unidad = d.UnidadMedida,
                        cantidad = d.Cantidad,
                        precio = d.PrecioUnitario,
                        total = d.TotalProducto
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo cargar el detalle de venta {IdVenta}", idVenta);
                return StatusCode(503, new { exito = false, mensaje = "No se pudo consultar el detalle de la venta." });
            }
        }

        /// <summary>
        /// Anula una venta (borrado lógico + reposición de stock). Solo rol EmpleadoVenta.
        /// </summary>
        [HttpPost("anular")]
        [Authorize(Roles = RolesSistema.EmpleadoVenta)]
        public async Task<IActionResult> Anular([FromBody] AnularVentaRequest request)
        {
            try
            {
                if (request.IdVenta <= 0)
                {
                    return BadRequest(new { exito = false, mensaje = "El identificador de la venta no es válido." });
                }

                if (string.IsNullOrWhiteSpace(request.Motivo))
                {
                    return BadRequest(new { exito = false, mensaje = "Debe indicar el motivo de la anulación." });
                }

                ResultadoVenta resultado = await _fachadaAnulacion.AnularVentaAsync(request.IdVenta, request.Motivo);

                if (!resultado.Exito)
                {
                    return BadRequest(new { exito = false, mensaje = resultado.Mensaje });
                }

                return Ok(new
                {
                    exito = true,
                    mensaje = $"Venta {resultado.CodigoVenta} anulada. El stock fue restaurado.",
                    codigoVenta = resultado.CodigoVenta
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo anular la venta {IdVenta}", request.IdVenta);
                return StatusCode(503, new { exito = false, mensaje = "No se pudo anular la venta." });
            }
        }

        /// <summary>
        /// Obtiene el comprobante de una venta. Cualquier usuario autenticado.
        /// </summary>
        [HttpGet("comprobante/{idVenta}")]
        public async Task<IActionResult> Comprobante(int idVenta)
        {
            try
            {
                ComprobanteVenta? comprobante = await _fachadaVenta.ObtenerComprobanteAsync(idVenta);

                if (comprobante is null)
                {
                    return NotFound(new { exito = false, mensaje = "Comprobante no encontrado." });
                }

                return Ok(new
                {
                    exito = true,
                    comprobante = new
                    {
                        venta = comprobante.Venta,
                        nombreCliente = comprobante.NombreCliente,
                        ciCliente = comprobante.CiCliente,
                        nombreEmpleadoResponsable = comprobante.NombreEmpleadoResponsable,
                        nombreEmpleadoAnulacion = comprobante.NombreEmpleadoAnulacion,
                        lineas = comprobante.Lineas.Select(d => new
                        {
                            nombre = d.Nombre,
                            codigo = d.Codigo,
                            unidad = d.UnidadMedida,
                            cantidad = d.Cantidad,
                            precio = d.PrecioUnitario,
                            total = d.TotalProducto
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo obtener el comprobante de venta {IdVenta}", idVenta);
                return StatusCode(503, new { exito = false, mensaje = "No se pudo obtener el comprobante." });
            }
        }

        /// <summary>
        /// Descarga el comprobante de una venta como PDF. Cualquier usuario autenticado.
        /// </summary>
        [HttpGet("comprobante/{idVenta}/pdf")]
        public async Task<IActionResult> ComprobantePdf(int idVenta)
        {
            try
            {
                ComprobanteVenta? comprobante = await _fachadaVenta.ObtenerComprobanteAsync(idVenta);

                if (comprobante is null)
                {
                    return NotFound(new { exito = false, mensaje = "Comprobante no encontrado." });
                }

                byte[] pdf = _generadorPdf.Generar(comprobante);
                string codigo = comprobante.Venta.CodigoVenta ?? comprobante.Venta.IdVenta.ToString();

                Response.Headers.Append("Content-Disposition", $"inline; filename=\"Comprobante-{codigo}.pdf\"");
                return File(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo generar el PDF del comprobante de venta {IdVenta}", idVenta);
                return StatusCode(503, new { exito = false, mensaje = "No se pudo generar el comprobante PDF." });
            }
        }
    }

    public class AnularVentaRequest
    {
        public int IdVenta { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
