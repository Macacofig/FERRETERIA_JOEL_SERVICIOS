using ServicioVentas.Aplicacion.Servicios;
using ServicioVentas.Dominio.Constantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServicioVentas.Controllers
{
    /// <summary>
    /// API de Productos para el módulo de ventas. Solo lectura: permite buscar
    /// productos disponibles para agregar a una venta.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductosController : ControllerBase
    {
        private readonly FachadaVenta _fachada;
        private readonly ILogger<ProductosController> _logger;

        public ProductosController(
            FachadaVenta fachada,
            ILogger<ProductosController> logger)
        {
            _fachada = fachada;
            _logger = logger;
        }

        /// <summary>
        /// Catálogo completo para la pantalla de consulta. Incluye productos
        /// activos e inactivos (opcionalmente solo activos). Administrador y
        /// EmpleadoVenta pueden consultarlo.
        /// </summary>
        [HttpGet("catalogo")]
        [Authorize(Roles = RolesSistema.Personal)]
        public async Task<IActionResult> Catalogo([FromQuery] bool soloActivos = false)
        {
            try
            {
                var productos = await _fachada.ListarProductosAsync();

                if (soloActivos)
                {
                    productos = productos.Where(p => p.Estado == 1).ToList();
                }

                var resultado = productos.Select(p => new
                {
                    id = p.IdProducto,
                    codigo = p.Codigo,
                    nombre = p.Nombre,
                    marca = p.NombreMarca,
                    categoria = p.NombreCategoria,
                    descripcion = p.Descripcion,
                    unidad = p.UnidadMedida,
                    precio = p.PrecioVenta,
                    stock = p.CantidadStock,
                    estado = p.Estado
                });

                return Ok(new { exito = true, productos = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el catálogo de productos");
                return StatusCode(503, new { exito = false, mensaje = "No se pudo obtener el catálogo de productos." });
            }
        }

        /// <summary>
        /// Busca productos por término (nombre o código). Solo rol EmpleadoVenta.
        /// </summary>
        [HttpGet("buscar")]
        [Authorize(Roles = RolesSistema.EmpleadoVenta)]
        public async Task<IActionResult> Buscar([FromQuery] string termino)
        {
            try
            {
                var productos = await _fachada.BuscarProductosAsync(termino ?? string.Empty);

                var resultado = productos.Select(p => new
                {
                    id = p.IdProducto,
                    codigo = p.Codigo,
                    nombre = p.Nombre,
                    unidad = p.UnidadMedida,
                    precio = p.PrecioVenta,
                    stock = p.CantidadStock
                });

                return Ok(new { exito = true, productos = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar productos desde Ventas");
                return StatusCode(503, new { exito = false, mensaje = "No se pudieron buscar productos." });
            }
        }

        /// <summary>
        /// Obtiene un producto por su ID para validar stock y precio antes de agregar a la venta.
        /// Solo rol EmpleadoVenta.
        /// </summary>
        [HttpGet("{idProducto}")]
        [Authorize(Roles = RolesSistema.EmpleadoVenta)]
        public async Task<IActionResult> Obtener(int idProducto)
        {
            try
            {
                var producto = await _fachada.ObtenerProductoAsync(idProducto);

                if (producto is null || producto.Estado != 1)
                {
                    return NotFound(new { exito = false, mensaje = "Producto no encontrado o inactivo." });
                }

                return Ok(new
                {
                    exito = true,
                    producto = new
                    {
                        id = producto.IdProducto,
                        codigo = producto.Codigo,
                        nombre = producto.Nombre,
                        unidad = producto.UnidadMedida,
                        precio = producto.PrecioVenta,
                        stock = producto.CantidadStock
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto {IdProducto}", idProducto);
                return StatusCode(503, new { exito = false, mensaje = "No se pudo obtener el producto." });
            }
        }
    }
}
