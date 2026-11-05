using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioReportes.Aplicacion;
using ServicioReportes.Dominio.Puertos;

namespace ServicioReportes.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Roles = "Administrador")]
public sealed class ReportesController : ControllerBase
{
    private readonly ReporteService _reportes;
    public ReportesController(ReporteService reportes) => _reportes = reportes;

    [HttpGet("productos-estables")]
    public Task<IReadOnlyList<ProductoEstable>> ProductosEstables(
        [FromQuery] decimal? precioMinimo, [FromQuery] decimal? precioMaximo,
        [FromQuery] int? cantidadVendidaMinima, CancellationToken cancellationToken)
        => _reportes.ObtenerProductosEstablesAsync(
            ValidarProductos(precioMinimo, precioMaximo, cantidadVendidaMinima), cancellationToken);

    [HttpGet("productos-estables/pdf")]
    public async Task<IActionResult> ProductosEstablesPdf(
        [FromQuery] decimal? precioMinimo, [FromQuery] decimal? precioMaximo,
        [FromQuery] int? cantidadVendidaMinima, CancellationToken cancellationToken)
    {
        var filtros = ValidarProductos(precioMinimo, precioMaximo, cantidadVendidaMinima);
        var pdf = await _reportes.GenerarProductosEstablesPdfAsync(filtros, UsuarioGenerador(), cancellationToken);
        return File(pdf, "application/pdf", $"productos-stock-estable-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
    }

    [HttpGet("productos-estables/excel")]
    public async Task<IActionResult> ProductosEstablesExcel(
        [FromQuery] decimal? precioMinimo, [FromQuery] decimal? precioMaximo,
        [FromQuery] int? cantidadVendidaMinima, CancellationToken cancellationToken)
    {
        var filtros = ValidarProductos(precioMinimo, precioMaximo, cantidadVendidaMinima);
        var excel = await _reportes.GenerarProductosEstablesExcelAsync(filtros, UsuarioGenerador(), cancellationToken);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"productos-stock-estable-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    [HttpGet("ventas-producto")]
    public Task<IReadOnlyList<VentaProducto>> VentasProducto(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] string? metodoPago, CancellationToken cancellationToken)
        => _reportes.ObtenerVentasPorProductoAsync(
            ValidarVentas(desde, hasta, metodoPago), cancellationToken);

    [HttpGet("ventas-producto/pdf")]
    public async Task<IActionResult> VentasProductoPdf([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] string? metodoPago, CancellationToken cancellationToken)
    {
        var filtros = ValidarVentas(desde, hasta, metodoPago);
        var pdf = await _reportes.GenerarVentasProductoPdfAsync(filtros, UsuarioGenerador(), cancellationToken);
        return File(pdf, "application/pdf", $"ventas-producto-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
    }

    [HttpGet("ventas-producto/excel")]
    public async Task<IActionResult> VentasProductoExcel([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] string? metodoPago, CancellationToken cancellationToken)
    {
        var filtros = ValidarVentas(desde, hasta, metodoPago);
        var excel = await _reportes.GenerarVentasProductoExcelAsync(filtros, UsuarioGenerador(), cancellationToken);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ventas-producto-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    private static ReporteProductoFiltros ValidarProductos(
        decimal? minimo, decimal? maximo, int? cantidad)
    {
        if (minimo.HasValue && maximo.HasValue && minimo.Value > maximo.Value)
            throw new ArgumentException("El precio mínimo no puede superar el precio máximo.");
        if (cantidad.HasValue && cantidad.Value < 0)
            throw new ArgumentException("La cantidad vendida mínima no puede ser negativa.");
        return new(minimo, maximo, cantidad);
    }

    private static ReporteVentaProductoFiltros ValidarVentas(
        DateTime? desde, DateTime? hasta, string? metodoPago)
    {
        var inicio = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var fin = hasta ?? inicio.AddMonths(1).AddDays(-1);
        if (fin.Date < inicio.Date) throw new ArgumentException("El rango de fechas no es válido.");
        var pago = string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago.Trim();
        if (!pago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase)
            && !pago.Equals("Tarjeta", StringComparison.OrdinalIgnoreCase)
            && !pago.Equals("QR/Transferencia", StringComparison.OrdinalIgnoreCase)
            && !pago.Equals("Transferencia", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El método de pago no es válido.");
        return new(inicio.Date, fin.Date,
            pago.Equals("QR/Transferencia", StringComparison.OrdinalIgnoreCase) ? "QR/Transferencia" : pago);
    }

    private string UsuarioGenerador() =>
        User.FindFirstValue(ClaimTypes.Name)
        ?? User.FindFirstValue("NombreCompleto")
        ?? User.Identity?.Name
        ?? "Usuario";
}
