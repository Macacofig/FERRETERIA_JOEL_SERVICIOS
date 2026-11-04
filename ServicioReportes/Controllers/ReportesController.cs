using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioReportes.Aplicacion;

namespace ServicioReportes.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize]
public sealed class ReportesController : ControllerBase
{
    private readonly ReporteService _reportes;

    public ReportesController(ReporteService reportes)
    {
        _reportes = reportes;
    }

    [HttpGet("ventas-detalle/pdf")]
    public async Task<IActionResult> VentasDetallePdf(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta,
        [FromQuery] int? empleadoId, [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        var filtros = ValidarFiltros(desde, hasta, empleadoId, estado, null);
        var pdf = await _reportes.GenerarDetallePdfAsync(filtros, UsuarioGenerador(), cancellationToken);
        return File(pdf, "application/pdf", $"ventas-detalladas-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
    }

    [HttpGet("ventas-detalle/excel")]
    public async Task<IActionResult> VentasDetalleExcel(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta,
        [FromQuery] int? empleadoId, [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        var filtros = ValidarFiltros(desde, hasta, empleadoId, estado, null);
        var excel = await _reportes.GenerarDetalleExcelAsync(filtros, UsuarioGenerador(), cancellationToken);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ventas-detalladas-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    [HttpGet("ventas-resumen/pdf")]
    public async Task<IActionResult> VentasResumenPdf(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta,
        [FromQuery] int? categoriaId, CancellationToken cancellationToken)
    {
        var filtros = ValidarFiltros(desde, hasta, null, null, categoriaId);
        var pdf = await _reportes.GenerarResumenPdfAsync(filtros, UsuarioGenerador(), cancellationToken);
        return File(pdf, "application/pdf", $"resumen-ventas-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
    }

    [HttpGet("ventas-resumen/excel")]
    public async Task<IActionResult> VentasResumenExcel(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta,
        [FromQuery] int? categoriaId, CancellationToken cancellationToken)
    {
        var filtros = ValidarFiltros(desde, hasta, null, null, categoriaId);
        var excel = await _reportes.GenerarResumenExcelAsync(filtros, UsuarioGenerador(), cancellationToken);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"resumen-ventas-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    private ReporteFiltros ValidarFiltros(DateTime desde, DateTime hasta, int? empleadoId, string? estado, int? categoriaId)
    {
        if (desde == default || hasta == default || hasta.Date < desde.Date)
            throw new ArgumentException("Debe indicar un rango de fechas válido.");
        return new ReporteFiltros(desde, hasta, empleadoId, estado?.Trim(), categoriaId);
    }

    private string UsuarioGenerador() =>
        User.FindFirstValue(ClaimTypes.Name)
        ?? User.FindFirstValue("NombreCompleto")
        ?? User.Identity?.Name
        ?? "Usuario";
}
