using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Helpers;
using FERRETERIA__Joel.Modelos;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Pantalla de registro de venta. La presentación solo habla con
    /// ServicioVentas mediante su cliente HTTP: nunca accede a repositorios
    /// ni transacciones. El precio y los totales siempre se resuelven en la
    /// API, enviando únicamente producto y cantidad.
    /// </summary>
    [Authorize(Roles = RolesSistema.EmpleadoVenta)]
    public class VentaModel : PageModel
    {
        private readonly VentasApiCliente _api;
        private readonly ILogger<VentaModel> _logger;

        public VentaModel(
            VentasApiCliente api,
            ILogger<VentaModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public IReadOnlyList<string> MetodosPago { get; } = new[]
        {
            "Efectivo", "Tarjeta", "Transferencia", "QR"
        };

        [BindProperty]
        public int IdCliente { get; set; }

        [BindProperty]
        public string? NombreCliente { get; set; }

        [BindProperty]
        public string MetodoPago { get; set; } = "Efectivo";

        [BindProperty]
        public List<LineaVentaSolicitada> Lineas { get; set; } = new();

        public string? MensajeError { get; set; }

        public async Task<IActionResult> OnGetBuscarClientesAsync(string? termino)
        {
            try
            {
                var clientes = (await _api.BuscarClientesAsync(
                        ObtenerToken(),
                        termino ?? string.Empty))
                    .Select(c => new
                    {
                        id = c.Id,
                        ci = c.Ci,
                        nombre = c.Nombre
                    });

                return new JsonResult(clientes);
            }
            catch (VentasApiException ex)
            {
                _logger.LogWarning(ex, "Error al buscar clientes desde Ventas");
                return StatusCode(ex.Estado, new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar clientes desde Ventas");
                return StatusCode(503, new
                {
                    mensaje = "No se pudieron buscar clientes. Revise la conexión con el servicio de ventas."
                });
            }
        }

        public async Task<IActionResult> OnGetBuscarProductosAsync(string? termino)
        {
            try
            {
                var productos = (await _api.BuscarProductosAsync(
                        ObtenerToken(),
                        termino ?? string.Empty))
                    .Select(p => new
                    {
                        id = p.Id,
                        codigo = p.Codigo,
                        nombre = p.Nombre,
                        unidad = p.Unidad,
                        precio = p.Precio,
                        stock = p.Stock
                    });

                return new JsonResult(productos);
            }
            catch (VentasApiException ex)
            {
                _logger.LogWarning(ex, "Error al buscar productos desde Ventas");
                return StatusCode(ex.Estado, new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar productos desde Ventas");
                return StatusCode(503, new
                {
                    mensaje = "No se pudieron buscar productos. Revise la conexión con el servicio de ventas."
                });
            }
        }

        /// <summary>
        /// Registra un cliente desde el modal "Nuevo cliente" de la propia
        /// pantalla de venta. Al ser una llamada AJAX la página nunca se
        /// recarga y las líneas seleccionadas se conservan.
        /// </summary>
        public async Task<IActionResult> OnPostCrearClienteAsync(
            string? nuevoClienteCi,
            string? nuevoClienteNombre,
            string? nuevoClienteApellido)
        {
            try
            {
                ResultadoVentaApi resultado = await _api.CrearClienteAsync(
                    ObtenerToken(),
                    new CrearClienteApi
                    {
                        Ci = nuevoClienteCi,
                        Nombre = nuevoClienteNombre,
                        Apellido = nuevoClienteApellido
                    });

                if (!resultado.Exito)
                {
                    return BadRequest(new
                    {
                        exito = false,
                        mensaje = resultado.Mensaje,
                        errores = resultado.Errores
                    });
                }

                return new JsonResult(new
                {
                    exito = true,
                    id = resultado.Id,
                    ci = resultado.Ci,
                    nombre = resultado.Nombre
                });
            }
            catch (VentasApiException ex)
            {
                _logger.LogWarning(ex, "Error al crear un cliente desde el modal de Ventas");
                return StatusCode(ex.Estado, new
                {
                    exito = false,
                    mensaje = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear un cliente desde el modal de Ventas");
                return StatusCode(503, new
                {
                    exito = false,
                    mensaje = "No se pudo guardar el cliente. Revise la conexión con el servicio de ventas."
                });
            }
        }

        public async Task<IActionResult> OnPostRegistrarAsync()
        {
            ResultadoVentaApi resultado;
            try
            {
                resultado = await _api.RegistrarVentaAsync(
                    ObtenerToken(),
                    ConstruirSolicitud());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado al registrar la venta");
                MensajeError = "No se pudo registrar la venta. Revise la conexión con el servicio de ventas.";
                return Page();
            }

            if (!resultado.Exito)
            {
                MensajeError = resultado.Mensaje;
                return Page();
            }

            TempData["Mensaje"] = resultado.Mensaje;
            return RedirectToPage("/Ventas");
        }

        public async Task<IActionResult> OnPostRegistrarAjaxAsync()
        {
            ResultadoVentaApi resultado;
            try
            {
                resultado = await _api.RegistrarVentaAsync(
                    ObtenerToken(),
                    ConstruirSolicitud());
            }
            catch (VentasApiException ex)
            {
                _logger.LogWarning(ex, "Error al registrar la venta desde Ventas");
                return StatusCode(ex.Estado, new
                {
                    exito = false,
                    mensaje = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado al registrar la venta");
                return StatusCode(503, new
                {
                    exito = false,
                    mensaje = "No se pudo registrar la venta. Revise la conexión con el servicio de ventas."
                });
            }

            if (!resultado.Exito)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = resultado.Mensaje
                });
            }

            string token = UrlProtector.Cifrar(resultado.IdVenta.ToString());

            TempData["Mensaje"] = resultado.Mensaje;

            return new JsonResult(new
            {
                exito = true,
                mensaje = resultado.Mensaje,
                urlPdf = Url.Page("/VentaComprobante", "Pdf", new { token })
            });
        }

        private RegistrarVentaApi ConstruirSolicitud()
        {
            // El estado del pago no se toma del cliente: lo fija el servidor.
            return new RegistrarVentaApi
            {
                IdCliente = IdCliente,
                MetodoPago = MetodoPago,
                EstadoPago = EstadoPago.Pagado,
                Lineas = Lineas
                    .Select(l => new LineaVentaApi
                    {
                        IdProducto = l.IdProducto,
                        Cantidad = l.Cantidad
                    })
                    .ToList()
            };
        }

        private string? ObtenerToken()
        {
            return HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);
        }
    }
}
