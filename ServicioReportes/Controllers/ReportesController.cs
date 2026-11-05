using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioReportes.Aplicacion;
using ServicioReportes.Dominio.Puertos;

namespace ServicioReportes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public sealed class ReportesController : ControllerBase
{
    private readonly ReporteService _reporteService;
    private readonly IReporteVentasRepository _repositorio;
    private readonly ILogger<ReportesController> _logger;

    public ReportesController(
        ReporteService reporteService,
        IReporteVentasRepository repositorio,
        ILogger<ReportesController> logger)
    {
        _reporteService = reporteService;
        _repositorio = repositorio;
        _logger = logger;
    }

    [HttpGet("categorias")]
    public IActionResult Categorias()
    {
        return Ok(new { exito = true, categorias = Array.Empty<object>() });
    }

    // ---------- REPORTE 1: INVENTARIO / STOCK ----------

    [HttpGet("ventas")]
    public async Task<IActionResult> Ventas(
        [FromQuery] decimal? precioMaximo,
        [FromQuery] int? stockMinimo,
        CancellationToken cancellationToken)
    {
        try
        {
            var filtros = new ReporteProductoFiltros(null, precioMaximo ?? 500m, null, stockMinimo ?? 10);
            var productos = await _repositorio.ObtenerProductosEstablesAsync(filtros, cancellationToken);

            var lineas = productos.Select(p => new
            {
                codigo = p.Codigo,
                producto = p.Nombre,
                precio = p.Precio,
                descripcion = p.Descripcion,
                marca = p.Marca,
                categoria = p.Categoria,
                cantidadStock = p.CantidadStock
            }).ToList();

            var totalRegistros = lineas.Count;
            var totalStock = lineas.Sum(l => l.cantidadStock);
            var precioPromedio = totalRegistros > 0 ? lineas.Average(l => l.precio) : 0m;

            return Ok(new
            {
                exito = true,
                reporte = new
                {
                    titulo = "Productos de stock estable y precio razonable",
                    filtro = new
                    {
                        stockMinimo = stockMinimo ?? 10,
                        precioMaximo = precioMaximo ?? 500m,
                        filtroTexto = $"Stock > {stockMinimo ?? 10} unidades, Precio < Bs {precioMaximo ?? 500m:N2}"
                    },
                    generadoPor = UsuarioActual(),
                    generadoPorCompleto = UsuarioActual(),
                    generadoEl = DateTime.Now,
                    totalRegistros,
                    totalStock,
                    precioPromedio,
                    lineas
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar reporte de productos estables");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { exito = false, mensaje = "Error interno al generar reporte." });
        }
    }

    [HttpGet("ventas/pdf")]
    public async Task<IActionResult> VentasPdf(
        [FromQuery] decimal? precioMaximo,
        [FromQuery] int? stockMinimo,
        CancellationToken cancellationToken)
    {
        var filtros = new ReporteProductoFiltros(null, precioMaximo ?? 500m, null, stockMinimo ?? 10);
        var bytes = await _reporteService.GenerarProductosEstablesPdfAsync(filtros, UsuarioActual(), cancellationToken);
        return File(bytes, "application/pdf", $"Productos-Estables-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
    }

    [HttpGet("ventas/excel")]
    public async Task<IActionResult> VentasExcel(
        [FromQuery] decimal? precioMaximo,
        [FromQuery] int? stockMinimo,
        CancellationToken cancellationToken)
    {
        var filtros = new ReporteProductoFiltros(null, precioMaximo ?? 500m, null, stockMinimo ?? 10);
        var bytes = await _reporteService.GenerarProductosEstablesExcelAsync(filtros, UsuarioActual(), cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Productos-Estables-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    // ---------- REPORTE 2: ESTADÍSTICAS / VENTAS SUMARIADAS ----------

    [HttpGet("estadisticas")]
    public async Task<IActionResult> Estadisticas(
        [FromQuery] int? cantidadMinima,
        [FromQuery] string? metodoPago,
        CancellationToken cancellationToken)
    {
        try
        {
            var metodo = string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago;
            var filtros = new ReporteVentaProductoFiltros(
                DateTime.Today.AddYears(-1), DateTime.Today.AddDays(1), metodo, cantidadMinima ?? 3);
            var ventas = await _repositorio.ObtenerVentasPorProductoAsync(filtros, cancellationToken);

            var filasFiltradas = ventas.ToList();

            var totalImporte = filasFiltradas.Sum(v => v.TotalProducto);
            var totalUnidades = filasFiltradas.Sum(v => v.Cantidad);

            var filas = filasFiltradas.Select(v => new
            {
                producto = v.Nombre,
                cantidadTotal = v.Cantidad,
                totalRecaudado = v.TotalProducto,
                participacion = totalImporte > 0 ? Math.Round((v.TotalProducto / totalImporte) * 100m, 2) : 0m
            }).ToList();

            return Ok(new
            {
                exito = true,
                reporte = new
                {
                    titulo = "Ventas por producto y medio de cobro",
                    filtro = new
                    {
                        cantidadMinima = cantidadMinima ?? 3,
                        metodoPago = metodo,
                        filtroTexto = $"Cantidad vendida >= {cantidadMinima ?? 3}, Método de cobro: {metodo}"
                    },
                    generadoPor = UsuarioActual(),
                    generadoPorCompleto = UsuarioActual(),
                    generadoEl = DateTime.Now,
                    totalProductos = filas.Count,
                    totalUnidades,
                    totalImporte,
                    filas
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar reporte estadístico");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { exito = false, mensaje = "Error interno al generar informe." });
        }
    }

    [HttpGet("estadisticas/pdf")]
    public async Task<IActionResult> EstadisticasPdf(
        [FromQuery] int? cantidadMinima,
        [FromQuery] string? metodoPago,
        CancellationToken cancellationToken)
    {
        try
        {
            var metodo = string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago;
            var filtros = new ReporteVentaProductoFiltros(
                DateTime.Today.AddYears(-1), DateTime.Today.AddDays(1), metodo, cantidadMinima ?? 3);
            var bytes = await _reporteService.GenerarVentasProductoPdfAsync(filtros, UsuarioActual(), cancellationToken);
            return File(bytes, "application/pdf", $"Ventas-Producto-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar el PDF del reporte estadístico");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { exito = false, mensaje = "No se pudo generar el PDF del informe. Intente nuevamente." });
        }
    }

    [HttpGet("estadisticas/excel")]
    public async Task<IActionResult> EstadisticasExcel(
        [FromQuery] int? cantidadMinima,
        [FromQuery] string? metodoPago,
        CancellationToken cancellationToken)
    {
        try
        {
            var metodo = string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago;
            var filtros = new ReporteVentaProductoFiltros(
                DateTime.Today.AddYears(-1), DateTime.Today.AddDays(1), metodo, cantidadMinima ?? 3);
            var bytes = await _reporteService.GenerarVentasProductoExcelAsync(filtros, UsuarioActual(), cancellationToken);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Ventas-Producto-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar el Excel del reporte estadístico");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { exito = false, mensaje = "No se pudo generar el Excel del informe. Intente nuevamente." });
        }
    }

    private string UsuarioActual() =>
        User.FindFirstValue(ClaimTypes.Name)
        ?? User.Identity?.Name
        ?? "Administrador";
}