using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Cliente HTTP del front hacia ServicioReportes. Centraliza las rutas,
    /// la serialización del filtro y la descarga de PDF/Excel. El JWT se toma
    /// de la sesión de servidor y viaja en la cabecera Authorization.
    /// </summary>
    public sealed class ReportesApiCliente
    {
        private static readonly JsonSerializerOptions OpcionesJson =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient _http;

        public ReportesApiCliente(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<CategoriaReporteApi>> ObtenerCategoriasAsync(
            string? token,
            CancellationToken cancelacion = default)
        {
            RespuestaCategoriasApi respuesta =
                await EnviarAsync<RespuestaCategoriasApi>(
                    "api/reportes/categorias",
                    token,
                    cancelacion);

            return respuesta.Categorias;
        }

        public async Task<ReporteVentasApi> ObtenerVentasAsync(
            string? token,
            FiltroConsultaReporte filtro,
            CancellationToken cancelacion = default)
        {
            RespuestaReporteVentasApi respuesta =
                await EnviarAsync<RespuestaReporteVentasApi>(
                    ConstruirRuta("api/reportes/ventas", filtro),
                    token,
                    cancelacion);

            return respuesta.Reporte
                ?? throw new ReportesApiException(
                    (int)HttpStatusCode.BadGateway,
                    "El servicio de reportes no devolvió el reporte de ventas.");
        }

        public async Task<ReporteEstadisticasApi> ObtenerEstadisticasAsync(
            string? token,
            FiltroConsultaReporte filtro,
            CancellationToken cancelacion = default)
        {
            RespuestaReporteEstadisticasApi respuesta =
                await EnviarAsync<RespuestaReporteEstadisticasApi>(
                    ConstruirRuta("api/reportes/estadisticas", filtro),
                    token,
                    cancelacion);

            return respuesta.Reporte
                ?? throw new ReportesApiException(
                    (int)HttpStatusCode.BadGateway,
                    "El servicio de reportes no devolvió el informe estadístico.");
        }

        public Task<ArchivoDescargaApi> DescargarVentasPdfAsync(
            string? token,
            FiltroConsultaReporte filtro,
            CancellationToken cancelacion = default)
        {
            return DescargarAsync(
                ConstruirRuta("api/reportes/ventas/pdf", filtro),
                token,
                "Reporte-Ventas.pdf",
                cancelacion);
        }

        public Task<ArchivoDescargaApi> DescargarVentasExcelAsync(
            string? token,
            FiltroConsultaReporte filtro,
            CancellationToken cancelacion = default)
        {
            return DescargarAsync(
                ConstruirRuta("api/reportes/ventas/excel", filtro),
                token,
                "Reporte-Ventas.xlsx",
                cancelacion);
        }

        public Task<ArchivoDescargaApi> DescargarEstadisticasPdfAsync(
            string? token,
            FiltroConsultaReporte filtro,
            CancellationToken cancelacion = default)
        {
            return DescargarAsync(
                ConstruirRuta("api/reportes/estadisticas/pdf", filtro),
                token,
                "Reporte-Estadisticas.pdf",
                cancelacion);
        }

        public Task<ArchivoDescargaApi> DescargarEstadisticasExcelAsync(
            string? token,
            FiltroConsultaReporte filtro,
            CancellationToken cancelacion = default)
        {
            return DescargarAsync(
                ConstruirRuta("api/reportes/estadisticas/excel", filtro),
                token,
                "Reporte-Estadisticas.xlsx",
                cancelacion);
        }

        // ---------- Infraestructura HTTP ----------

        private async Task<T> EnviarAsync<T>(
            string ruta,
            string? token,
            CancellationToken cancelacion)
            where T : class
        {
            using HttpRequestMessage peticion = new(HttpMethod.Get, ruta);
            AplicarToken(peticion, token);

            using HttpResponseMessage respuesta =
                await _http.SendAsync(peticion, cancelacion);

            string texto = await respuesta.Content.ReadAsStringAsync(cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                throw new ReportesApiException(
                    (int)respuesta.StatusCode,
                    LeerMensaje(texto)
                        ?? $"El servicio de reportes respondió con el código "
                        + $"{(int)respuesta.StatusCode}.");
            }

            T? contenido = Deserializar<T>(texto);

            if (contenido is null)
            {
                throw new ReportesApiException(
                    (int)HttpStatusCode.BadGateway,
                    "El servicio de reportes devolvió una respuesta vacía.");
            }

            return contenido;
        }

        private async Task<ArchivoDescargaApi> DescargarAsync(
            string ruta,
            string? token,
            string nombrePredeterminado,
            CancellationToken cancelacion)
        {
            using HttpRequestMessage peticion = new(HttpMethod.Get, ruta);
            AplicarToken(peticion, token);

            using HttpResponseMessage respuesta =
                await _http.SendAsync(peticion, cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                string texto = await respuesta.Content.ReadAsStringAsync(cancelacion);

                throw new ReportesApiException(
                    (int)respuesta.StatusCode,
                    LeerMensaje(texto)
                        ?? $"El servicio de reportes respondió con el código "
                        + $"{(int)respuesta.StatusCode}.");
            }

            byte[] contenido =
                await respuesta.Content.ReadAsByteArrayAsync(cancelacion);

            string nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
                ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? nombrePredeterminado;

            string tipo = respuesta.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";

            return new ArchivoDescargaApi(contenido, nombre, tipo);
        }

        private static string ConstruirRuta(
            string ruta,
            FiltroConsultaReporte filtro)
        {
            List<string> parametros = new();

            if (filtro.StockMinimo is int stock)
                parametros.Add("stockMinimo=" + stock.ToString(CultureInfo.InvariantCulture));
            if (filtro.PrecioMaximo is decimal precio)
                parametros.Add("precioMaximo=" + precio.ToString(CultureInfo.InvariantCulture));
            if (filtro.CantidadMinima is int cantidad)
                parametros.Add("cantidadMinima=" + cantidad.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(filtro.MetodoPago))
                parametros.Add("metodoPago=" + Uri.EscapeDataString(filtro.MetodoPago.Trim()));

            return parametros.Count == 0
                ? ruta
                : ruta + "?" + string.Join("&", parametros);
        }

        private static void AplicarToken(
            HttpRequestMessage peticion,
            string? token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                peticion.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        private static T? Deserializar<T>(string texto)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(texto, OpcionesJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? LeerMensaje(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            try
            {
                using JsonDocument documento = JsonDocument.Parse(texto);

                if (documento.RootElement.ValueKind == JsonValueKind.Object
                    && documento.RootElement.TryGetProperty("mensaje", out JsonElement mensaje)
                    && mensaje.ValueKind == JsonValueKind.String)
                {
                    return mensaje.GetString();
                }
            }
            catch (JsonException)
            {
                // Sin mensaje legible: se usa el código HTTP.
            }

            return null;
        }
    }
}
