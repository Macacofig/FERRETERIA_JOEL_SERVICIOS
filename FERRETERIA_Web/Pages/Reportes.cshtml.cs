using System.Globalization;
using System.Net.Http.Headers;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages;

[Authorize(Roles = RolesSistema.Administrador)]
public sealed class ReportesModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReportesModel> _logger;

    public ReportesModel(IHttpClientFactory httpClientFactory, ILogger<ReportesModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)] public DateTime Desde { get; set; } = DateTime.Today.AddDays(-30);
    [BindProperty(SupportsGet = true)] public DateTime Hasta { get; set; } = DateTime.Today;
    [BindProperty(SupportsGet = true)] public int? EmpleadoId { get; set; }
    [BindProperty(SupportsGet = true)] public string Estado { get; set; } = "Registrada";
    [BindProperty(SupportsGet = true)] public int? CategoriaId { get; set; }

    public void OnGet() => ViewData["ActiveMenu"] = "";

    public async Task<IActionResult> OnPostDetallePdfAsync()
        => await DescargarAsync("ventas-detalle/pdf", "application/pdf", $"ventas-detalladas-{DateTime.Now:yyyyMMdd}.pdf",
            new[] { ObtenerEmpleadoFiltro(), ObtenerEstadoFiltro() });

    public async Task<IActionResult> OnPostDetalleExcelAsync()
        => await DescargarAsync("ventas-detalle/excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ventas-detalladas-{DateTime.Now:yyyyMMdd}.xlsx",
            new[] { ObtenerEmpleadoFiltro(), ObtenerEstadoFiltro() });

    public async Task<IActionResult> OnPostResumenPdfAsync()
        => await DescargarAsync("ventas-resumen/pdf", "application/pdf",
            $"resumen-ventas-{DateTime.Now:yyyyMMdd}.pdf", new[] { ObtenerCategoriaFiltro() });

    public async Task<IActionResult> OnPostResumenExcelAsync()
        => await DescargarAsync("ventas-resumen/excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"resumen-ventas-{DateTime.Now:yyyyMMdd}.xlsx", new[] { ObtenerCategoriaFiltro() });

    private string? ObtenerEmpleadoFiltro() =>
        EmpleadoId.HasValue ? $"empleadoId={EmpleadoId.Value}" : null;
    private string? ObtenerEstadoFiltro() =>
        string.IsNullOrWhiteSpace(Estado) ? null : $"estado={Uri.EscapeDataString(Estado)}";
    private string? ObtenerCategoriaFiltro() =>
        CategoriaId.HasValue ? $"categoriaId={CategoriaId.Value}" : null;

    private async Task<IActionResult> DescargarAsync(
        string ruta, string contentType, string nombreArchivo, IEnumerable<string?> filtrosExtra)
    {
        if (Hasta.Date < Desde.Date)
        {
            TempData["Mensaje"] = "La fecha hasta debe ser igual o posterior a la fecha desde.";
            return RedirectToPage();
        }

        var token = HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToPage("/Login");

        try
        {
            var cliente = _httpClientFactory.CreateClient("ReportesApi");
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var parametros = new List<string>
            {
                $"desde={Desde:yyyy-MM-dd}",
                $"hasta={Hasta:yyyy-MM-dd}"
            };
            parametros.AddRange(filtrosExtra.Where(filtro => !string.IsNullOrWhiteSpace(filtro))!);
            var respuesta = await cliente.GetAsync($"api/reportes/{ruta}?{string.Join("&", parametros)}");
            if (!respuesta.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "No se pudo generar el reporte solicitado.";
                return RedirectToPage();
            }
            var archivo = await respuesta.Content.ReadAsByteArrayAsync();
            return File(archivo, contentType, nombreArchivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descargar el reporte desde ServicioReportes.");
            TempData["Mensaje"] = "No se pudo conectar con el servicio de reportes. Inténtelo nuevamente.";
            return RedirectToPage();
        }
    }
}
