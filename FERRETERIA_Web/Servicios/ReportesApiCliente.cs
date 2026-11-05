using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace FERRETERIA__Joel.Servicios;

public sealed class ReportesApiCliente
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<ReportesApiCliente> _logger;

    public ReportesApiCliente(HttpClient http, ILogger<ReportesApiCliente> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CategoriaOpcionApi>> ObtenerCategoriasAsync(
        string? token,
        CancellationToken cancelacion = default)
    {
        using HttpRequestMessage peticion = new(HttpMethod.Get, "api/reportes/categorias");
        AdjuntarToken(peticion, token);

        using HttpResponseMessage respuesta = await _http.SendAsync(
            peticion, HttpCompletionOption.ResponseHeadersRead, cancelacion);

        await AsegurarExitoAsync(respuesta, "No se pudieron obtener las categorías.", cancelacion);

        using Stream flujo = await respuesta.Content.ReadAsStreamAsync(cancelacion);
        RespuestaCategoriasApi? datos = await JsonSerializer.DeserializeAsync<RespuestaCategoriasApi>(
            flujo, OpcionesJson, cancelacion);

        return (IReadOnlyList<CategoriaOpcionApi>?)datos?.Categorias ?? Array.Empty<CategoriaOpcionApi>();
    }

    public async Task<ReporteVentasApi> ObtenerVentasAsync(
        string? token,
        FiltroConsultaReporte filtro,
        CancellationToken cancelacion = default)
    {
        string uri = ConstruirUri("api/reportes/ventas", filtro);
        using HttpRequestMessage peticion = new(HttpMethod.Get, uri);
        AdjuntarToken(peticion, token);

        using HttpResponseMessage respuesta = await _http.SendAsync(
            peticion, HttpCompletionOption.ResponseHeadersRead, cancelacion);

        await AsegurarExitoAsync(respuesta, "No se pudo cargar el reporte de inventario.", cancelacion);

        using Stream flujo = await respuesta.Content.ReadAsStreamAsync(cancelacion);
        RespuestaVentasApi? datos = await JsonSerializer.DeserializeAsync<RespuestaVentasApi>(
            flujo, OpcionesJson, cancelacion);

        return datos?.Reporte
            ?? throw new ReportesApiException("El servicio de reportes devolvió una respuesta vacía.");
    }

    public Task<ArchivoDescargaApi> DescargarVentasPdfAsync(
        string? token,
        FiltroConsultaReporte filtro,
        CancellationToken cancelacion = default)
    {
        return DescargarArchivoAsync(
            ConstruirUri("api/reportes/ventas/pdf", filtro),
            token,
            "No se pudo descargar el PDF de stock.",
            cancelacion);
    }

    public Task<ArchivoDescargaApi> DescargarVentasExcelAsync(
        string? token,
        FiltroConsultaReporte filtro,
        CancellationToken cancelacion = default)
    {
        return DescargarArchivoAsync(
            ConstruirUri("api/reportes/ventas/excel", filtro),
            token,
            "No se pudo descargar el Excel de stock.",
            cancelacion);
    }

    public async Task<ReporteEstadisticasApi> ObtenerEstadisticasAsync(
        string? token,
        FiltroConsultaReporte filtro,
        CancellationToken cancelacion = default)
    {
        string uri = ConstruirUri("api/reportes/estadisticas", filtro);
        using HttpRequestMessage peticion = new(HttpMethod.Get, uri);
        AdjuntarToken(peticion, token);

        using HttpResponseMessage respuesta = await _http.SendAsync(
            peticion, HttpCompletionOption.ResponseHeadersRead, cancelacion);

        await AsegurarExitoAsync(respuesta, "No se pudo cargar el reporte sumariado.", cancelacion);

        using Stream flujo = await respuesta.Content.ReadAsStreamAsync(cancelacion);
        RespuestaEstadisticasApi? datos = await JsonSerializer.DeserializeAsync<RespuestaEstadisticasApi>(
            flujo, OpcionesJson, cancelacion);

        return datos?.Reporte
            ?? throw new ReportesApiException("El servicio de reportes devolvió una respuesta vacía.");
    }

    public Task<ArchivoDescargaApi> DescargarEstadisticasPdfAsync(
        string? token,
        FiltroConsultaReporte filtro,
        CancellationToken cancelacion = default)
    {
        return DescargarArchivoAsync(
            ConstruirUri("api/reportes/estadisticas/pdf", filtro),
            token,
            "No se pudo descargar el PDF de estadísticas.",
            cancelacion);
    }

    public Task<ArchivoDescargaApi> DescargarEstadisticasExcelAsync(
        string? token,
        FiltroConsultaReporte filtro,
        CancellationToken cancelacion = default)
    {
        return DescargarArchivoAsync(
            ConstruirUri("api/reportes/estadisticas/excel", filtro),
            token,
            "No se pudo descargar el Excel de estadísticas.",
            cancelacion);
    }

    private async Task<ArchivoDescargaApi> DescargarArchivoAsync(
        string uri,
        string? token,
        string mensajeFallo,
        CancellationToken cancelacion)
    {
        using HttpRequestMessage peticion = new(HttpMethod.Get, uri);
        AdjuntarToken(peticion, token);

        using HttpResponseMessage respuesta = await _http.SendAsync(
            peticion, HttpCompletionOption.ResponseHeadersRead, cancelacion);

        await AsegurarExitoAsync(respuesta, mensajeFallo, cancelacion);

        byte[] contenido = await respuesta.Content.ReadAsByteArrayAsync(cancelacion);
        string tipoContenido = respuesta.Content.Headers.ContentType?.MediaType
            ?? "application/octet-stream";
        string nombreArchivo = respuesta.Content.Headers.ContentDisposition?.FileNameStar
            ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "reporte";

        return new ArchivoDescargaApi(contenido, tipoContenido, nombreArchivo);
    }

    private static string ConstruirUri(string ruta, FiltroConsultaReporte filtro)
    {
        Dictionary<string, string?> parametros = new();

        if (filtro.StockMinimo.HasValue)
            parametros["stockMinimo"] = filtro.StockMinimo.Value.ToString();

        if (filtro.PrecioMaximo.HasValue)
            parametros["precioMaximo"] = filtro.PrecioMaximo.Value.ToString();

        if (filtro.CantidadMinima.HasValue)
            parametros["cantidadMinima"] = filtro.CantidadMinima.Value.ToString();

        if (!string.IsNullOrWhiteSpace(filtro.MetodoPago))
            parametros["metodoPago"] = filtro.MetodoPago;

        return QueryHelpers.AddQueryString(ruta, parametros);
    }

    private static void AdjuntarToken(HttpRequestMessage peticion, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private async Task AsegurarExitoAsync(
        HttpResponseMessage respuesta,
        string mensajePorDefecto,
        CancellationToken cancelacion)
    {
        if (respuesta.IsSuccessStatusCode) return;

        string cuerpo = string.Empty;
        try
        {
            cuerpo = await respuesta.Content.ReadAsStringAsync(cancelacion);
            using JsonDocument documento = JsonDocument.Parse(cuerpo);
            if (documento.RootElement.TryGetProperty("mensaje", out JsonElement propMensaje))
            {
                string? mensajeServidor = propMensaje.GetString();
                if (!string.IsNullOrWhiteSpace(mensajeServidor))
                {
                    throw new ReportesApiException(mensajeServidor);
                }
            }
        }
        catch (JsonException) { }
        catch (ReportesApiException) { throw; }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No se pudo interpretar el error JSON del servicio de reportes.");
        }

        if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new ReportesApiException("Su sesión ha expirado o no tiene permisos.");

        throw new ReportesApiException($"{mensajePorDefecto} (HTTP {(int)respuesta.StatusCode})");
    }
}

public class ReportesApiException : Exception
{
    public ReportesApiException(string mensaje) : base(mensaje) { }
}