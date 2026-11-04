using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FERRETERIA__Joel.Pages.Reportes;

[Authorize(Roles = RolesSistema.Personal)]
public sealed class VentasModel : ReportePageModelBase
{
    public VentasModel(ReportesApiCliente api, ILogger<VentasModel> logger) : base(api, logger) { }

    [BindProperty(SupportsGet = true)] public int StockMinimo { get; set; } = 10;
    [BindProperty(SupportsGet = true)] public decimal PrecioMaximo { get; set; } = 500m;
    public ReporteVentasApi? Reporte { get; private set; }

    public async Task OnGetAsync()
    {
        if (StockMinimo < 0) StockMinimo = 10;
        if (PrecioMaximo <= 0) PrecioMaximo = 500m;
        try { Reporte = await Api.ObtenerVentasAsync(ObtenerToken(), Filtro()); }
        catch (ReportesApiException ex) { RegistrarError(ex.Message); }
        catch (Exception ex) { Logger.LogError(ex, "No se pudo cargar el reporte de stock."); RegistrarError("No se pudo generar el reporte de stock."); }
    }

    public Task<IActionResult> OnGetPdfAsync() => DescargarAsync(
        token => Api.DescargarVentasPdfAsync(token, Filtro()), "PDF", Valores());

    public Task<IActionResult> OnGetExcelAsync() => DescargarAsync(
        token => Api.DescargarVentasExcelAsync(token, Filtro()), "Excel", Valores());

    private FiltroConsultaReporte Filtro() => new() { StockMinimo = StockMinimo, PrecioMaximo = PrecioMaximo };
    private object Valores() => new { StockMinimo, PrecioMaximo };
}
