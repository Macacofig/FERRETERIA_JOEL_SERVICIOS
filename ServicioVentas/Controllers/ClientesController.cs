using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.Servicios;
using ServicioVentas.Dominio.Constantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServicioVentas.Controllers
{
    /// <summary>
    /// API de Clientes para el módulo de ventas. Permite buscar y crear
    /// clientes desde el flujo de venta sin salir de la pantalla.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly FachadaVenta _fachada;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            FachadaVenta fachada,
            ILogger<ClientesController> logger)
        {
            _fachada = fachada;
            _logger = logger;
        }

        /// <summary>
        /// Busca clientes por CI, nombre o apellido. Solo rol EmpleadoVenta.
        /// </summary>
        [HttpGet("buscar")]
        [Authorize(Roles = RolesSistema.EmpleadoVenta)]
        public async Task<IActionResult> Buscar([FromQuery] string termino)
        {
            try
            {
                var clientes = await _fachada.BuscarClientesAsync(termino ?? string.Empty);

                var resultado = clientes.Select(c => new
                {
                    id = c.IdCliente,
                    ci = c.Ci,
                    nombre = $"{c.Nombre} {c.Apellido}".Trim()
                });

                return Ok(new { exito = true, clientes = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar clientes desde Ventas");
                return StatusCode(503, new { exito = false, mensaje = "No se pudieron buscar clientes." });
            }
        }

        /// <summary>
        /// Crea un cliente rápido desde el modal de venta. Solo rol EmpleadoVenta.
        /// </summary>
        [HttpPost("crear")]
        [Authorize(Roles = RolesSistema.EmpleadoVenta)]
        public async Task<IActionResult> Crear([FromBody] CrearClienteRequest request)
        {
            try
            {
                ResultadoCliente resultado = await _fachada.CrearClienteAsync(
                    request.Ci ?? string.Empty,
                    request.Nombre ?? string.Empty,
                    request.Apellido ?? string.Empty);

                if (!resultado.Exito || resultado.Cliente is null)
                {
                    return BadRequest(new
                    {
                        exito = false,
                        mensaje = resultado.Mensaje,
                        errores = resultado.Errores
                    });
                }

                return Ok(new
                {
                    exito = true,
                    id = resultado.Cliente.IdCliente,
                    ci = resultado.Cliente.Ci,
                    nombre = $"{resultado.Cliente.Nombre} {resultado.Cliente.Apellido}".Trim()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cliente desde el modal de Ventas");
                return StatusCode(503, new
                {
                    exito = false,
                    mensaje = "No se pudo guardar el cliente."
                });
            }
        }
    }

    public class CrearClienteRequest
    {
        public string? Ci { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
    }
}
