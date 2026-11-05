using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FERRETERIA__Joel.Servicios;

public sealed class ReportesApiCliente
{
    private readonly HttpClient _http;
    public ReportesApiCliente(HttpClient http) => _http = http;

    public Task<ResultadoReporteApi<List<ProductoEstableApi>>> ConsultarProductosAsync(
        string token, decimal? precioMinimo, decimal? precioMaximo, int? cantidadVendidaMinima,
        CancellationToken cancellationToken = default)
        => GetAsync<List<ProductoEstableApi>>(
            ConstruirRuta("productos-estables", null, precioMinimo, precioMaximo, cantidadVendidaMinima, null),
            token, cancellationToken);

    public Task<ResultadoReporteApi<List<VentaProductoApi>>> ConsultarVentasAsync(
        string token, DateTime desde, DateTime hasta, string metodoPago,
        CancellationToken cancellationToken = default)
        => GetAsync<List<VentaProductoApi>>(
            $"api/reportes/ventas-producto?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}&metodoPago={Uri.EscapeDataString(metodoPago)}",
            token, cancellationToken);

    public async Task<ResultadoArchivoApi> DescargarAsync(string ruta, string token,
        CancellationToken cancellationToken = default)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuesta = await _http.GetAsync(ruta, cancellationToken);
        if (respuesta.IsSuccessStatusCode)
            return new ResultadoArchivoApi(true, await respuesta.Content.ReadAsByteArrayAsync(cancellationToken), null);
        return new ResultadoArchivoApi(false, null, await LeerMensajeErrorAsync(respuesta, cancellationToken));
    }

    private async Task<ResultadoReporteApi<T>> GetAsync<T>(string ruta, string token, CancellationToken cancellationToken)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuesta = await _http.GetAsync(ruta, cancellationToken);
        if (!respuesta.IsSuccessStatusCode)
            return new ResultadoReporteApi<T>(false, default, await LeerMensajeErrorAsync(respuesta, cancellationToken));
        return new ResultadoReporteApi<T>(true,
            await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken), null);
    }

    private static async Task<string> LeerMensajeErrorAsync(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        var contenido = await respuesta.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var json = JsonDocument.Parse(contenido);
            if (json.RootElement.TryGetProperty("mensaje", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
                return mensaje.GetString() ?? "La solicitud del reporte no es válida.";
            if (json.RootElement.TryGetProperty("title", out var titulo) && titulo.ValueKind == JsonValueKind.String)
                return titulo.GetString() ?? "La solicitud del reporte no es válida.";
        }
        catch (JsonException) { }
        return string.IsNullOrWhiteSpace(contenido)
            ? "La solicitud del reporte no es válida."
            : contenido.Trim();
    }

    private static string ConstruirRuta(string baseRuta, DateTime? desde, decimal? precioMinimo,
        decimal? precioMaximo, int? cantidadVendidaMinima, string? metodoPago)
    {
        var parametros = new List<string>();
        if (precioMinimo.HasValue) parametros.Add($"precioMinimo={precioMinimo.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (precioMaximo.HasValue) parametros.Add($"precioMaximo={precioMaximo.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (cantidadVendidaMinima.HasValue) parametros.Add($"cantidadVendidaMinima={cantidadVendidaMinima.Value}");
        return $"api/reportes/{baseRuta}" + (parametros.Count == 0 ? "" : "?" + string.Join("&", parametros));
    }
}

public sealed record ProductoEstableApi(
    string Codigo, string Nombre, decimal CostoUnitario, string Descripcion, string Marca, string Categoria);
public sealed record VentaProductoApi(string Nombre, int Cantidad, decimal TotalProducto);
public sealed record ResultadoReporteApi<T>(bool Exito, T? Datos, string? Mensaje);
public sealed record ResultadoArchivoApi(bool Exito, byte[]? Datos, string? Mensaje);
