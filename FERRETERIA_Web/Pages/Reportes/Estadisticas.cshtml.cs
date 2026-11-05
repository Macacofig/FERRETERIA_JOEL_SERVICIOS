using System.Text.Json;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FERRETERIA__Joel.Pages.Reportes;

[Authorize(Roles = RolesSistema.Personal)]
public sealed class EstadisticasModel : ReportePageModelBase
{
    public EstadisticasModel(ReportesApiCliente api, ILogger<EstadisticasModel> logger) : base(api, logger) { }

    [BindProperty(SupportsGet = true)] public int CantidadMinima { get; set; } = 3;
    [BindProperty(SupportsGet = true)] public string MetodoPago { get; set; } = "Efectivo";
    public ReporteEstadisticasApi? Reporte { get; private set; }
    public string DatosGraficoJson { get; private set; } = "{\"etiquetas\":[],\"valores\":[]}";

    public async Task OnGetAsync()
    {
        if (CantidadMinima < 0) CantidadMinima = 3;
        if (string.IsNullOrWhiteSpace(MetodoPago)) MetodoPago = "Efectivo";
        try
        {
            Reporte = await Api.ObtenerEstadisticasAsync(ObtenerToken(), Filtro());
            DatosGraficoJson = JsonSerializer.Serialize(new
            {
                etiquetas = Reporte.Filas.Select(x => x.Producto),
                valores = Reporte.Filas.Select(x => x.TotalRecaudado)
            });
        }
        catch (ReportesApiException ex) { RegistrarError(ex.Message); }
        catch (Exception ex) { Logger.LogError(ex, "No se pudo cargar el reporte sumariado."); RegistrarError("No se pudo generar el reporte sumariado."); }
    }

    public Task<IActionResult> OnGetPdfAsync() => DescargarAsync(
        token => Api.DescargarEstadisticasPdfAsync(token, Filtro()), "PDF", Valores());

    public Task<IActionResult> OnGetExcelAsync() => DescargarAsync(
        token => Api.DescargarEstadisticasExcelAsync(token, Filtro()), "Excel", Valores());

    private FiltroConsultaReporte Filtro() => new() { CantidadMinima = CantidadMinima, MetodoPago = MetodoPago };
    private object Valores() => new { CantidadMinima, MetodoPago };
}
