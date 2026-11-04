using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages.Reportes;

public abstract class ReportePageModelBase : PageModel
{
    protected ReportePageModelBase(ReportesApiCliente api, ILogger logger)
    {
        Api = api;
        Logger = logger;
    }

    public string? MensajeError { get; private set; }
    protected ReportesApiCliente Api { get; }
    protected ILogger Logger { get; }

    protected string? ObtenerToken() => HttpContext.Session.GetString(
        UsuariosApiCliente.ClaveSesionJwt);

    protected void RegistrarError(string mensaje) => MensajeError ??= mensaje;

    protected async Task<IActionResult> DescargarAsync(
        Func<string?, Task<ArchivoDescargaApi>> descargar, string formato,
        object valoresFiltro)
    {
        try
        {
            ArchivoDescargaApi archivo = await descargar(ObtenerToken());
            return File(archivo.Contenido, archivo.TipoContenido, archivo.NombreArchivo);
        }
        catch (ReportesApiException ex)
        {
            Logger.LogWarning(ex, "No se pudo descargar el reporte en {Formato}.", formato);
            TempData["MensajeError"] = ex.Message;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "No se pudo descargar el reporte en {Formato}.", formato);
            TempData["MensajeError"] = $"No se pudo generar el archivo {formato}.";
        }

        return RedirectToPage(null, valoresFiltro);
    }
}
