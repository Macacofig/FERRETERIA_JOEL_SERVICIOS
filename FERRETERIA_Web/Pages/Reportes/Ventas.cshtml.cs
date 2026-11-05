using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FERRETERIA__Joel.Pages.Reportes;

[Authorize(Roles = RolesSistema.Administrador)]
public sealed class VentasModel : ReportePageModelBase
{
    public VentasModel(ReportesApiCliente api, ILogger<VentasModel> logger) : base(api, logger) { }

    [BindProperty(SupportsGet = true)]
    public int? StockMinimo { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? PrecioMaximo { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Generar { get; set; }

    public ReporteVentasApi? Reporte { get; private set; }

    public async Task OnGetAsync()
    {
        CargarMensajeErrorTemporal();

        // Estado inicial vacío: no generar consulta al abrir la página por primera vez
        if (!Generar && !StockMinimo.HasValue && !PrecioMaximo.HasValue)
        {
            StockMinimo = 10;
            PrecioMaximo = 500m;
            return;
        }

        StockMinimo ??= 10;
        PrecioMaximo ??= 500m;

        if (StockMinimo < 0) StockMinimo = 10;
        if (PrecioMaximo <= 0) PrecioMaximo = 500m;

        try
        {
            Reporte = await Api.ObtenerVentasAsync(ObtenerToken(), Filtro());
        }
        catch (ReportesApiException ex) { RegistrarError(ex.Message); }
        catch (Exception ex)
        {
            Logger.LogError(ex, "No se pudo cargar el reporte de inventario.");
            RegistrarError("No se pudo generar el reporte de inventario.");
        }
    }

    public Task<IActionResult> OnGetPdfAsync() => DescargarAsync(
        token => Api.DescargarVentasPdfAsync(token, Filtro()), "PDF", Valores());

    public Task<IActionResult> OnGetExcelAsync() => DescargarAsync(
        token => Api.DescargarVentasExcelAsync(token, Filtro()), "Excel", Valores());

    private FiltroConsultaReporte Filtro() => new()
    {
        StockMinimo = StockMinimo ?? 10,
        PrecioMaximo = PrecioMaximo ?? 500m
    };

    private object Valores() => new
    {
        StockMinimo = StockMinimo ?? 10,
        PrecioMaximo = PrecioMaximo ?? 500m,
        Generar = true
    };
}