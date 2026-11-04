using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Cliente HTTP del front hacia ServicioVentas. Es el único punto del
    /// proyecto Razor que conoce la URL de la API de ventas y el formato de
    /// sus respuestas. El JWT viaja en la cabecera Authorization y se toma
    /// de la sesión de servidor, nunca de localStorage.
    /// </summary>
    public class VentasApiCliente
    {
        private static readonly JsonSerializerOptions OpcionesJson =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient _http;

        public VentasApiCliente(HttpClient http)
        {
            _http = http;
        }

        // ---------- Clientes ----------

        public async Task<List<ClienteBusquedaApi>> BuscarClientesAsync(
            string? token,
            string termino,
            CancellationToken cancelacion = default)
        {
            RespuestaClientes respuesta =
                await EnviarAsync<RespuestaClientes>(
                    HttpMethod.Get,
                    $"api/clientes/buscar?termino={Uri.EscapeDataString(termino ?? string.Empty)}",
                    token,
                    null,
                    cancelacion);

            return respuesta.Clientes;
        }

        public Task<ResultadoVentaApi> CrearClienteAsync(
            string? token,
            CrearClienteApi solicitud,
            CancellationToken cancelacion = default)
        {
            return EnviarOperacionAsync(
                "api/clientes/crear",
                token,
                solicitud,
                cancelacion);
        }

        // ---------- Productos ----------

        public async Task<List<ProductoBusquedaApi>> BuscarProductosAsync(
            string? token,
            string termino,
            CancellationToken cancelacion = default)
        {
            RespuestaProductos respuesta =
                await EnviarAsync<RespuestaProductos>(
                    HttpMethod.Get,
                    $"api/productos/buscar?termino={Uri.EscapeDataString(termino ?? string.Empty)}",
                    token,
                    null,
                    cancelacion);

            return respuesta.Productos;
        }

        public async Task<List<ProductoCatalogoApi>> ObtenerCatalogoAsync(
            string? token,
            bool soloActivos = false,
            CancellationToken cancelacion = default)
        {
            RespuestaCatalogo respuesta =
                await EnviarAsync<RespuestaCatalogo>(
                    HttpMethod.Get,
                    $"api/productos/catalogo?soloActivos={soloActivos.ToString().ToLowerInvariant()}",
                    token,
                    null,
                    cancelacion);

            return respuesta.Productos;
        }

        // ---------- Ventas ----------

        public Task<ResultadoVentaApi> RegistrarVentaAsync(
            string? token,
            RegistrarVentaApi solicitud,
            CancellationToken cancelacion = default)
        {
            return EnviarOperacionAsync(
                "api/ventas/registrar",
                token,
                solicitud,
                cancelacion);
        }

        public async Task<List<VentaListadoApi>> ListarVentasAsync(
            string? token,
            CancellationToken cancelacion = default)
        {
            RespuestaVentas respuesta =
                await EnviarAsync<RespuestaVentas>(
                    HttpMethod.Get,
                    "api/ventas/historial",
                    token,
                    null,
                    cancelacion);

            return respuesta.Ventas;
        }

        public Task<DetalleVentaApi?> ObtenerDetalleAsync(
            string? token,
            int idVenta,
            CancellationToken cancelacion = default)
        {
            return EnviarAsync<DetalleVentaApi?>(
                HttpMethod.Get,
                $"api/ventas/detalle/{idVenta}",
                token,
                null,
                cancelacion);
        }

        public Task<ResultadoVentaApi> AnularVentaAsync(
            string? token,
            int idVenta,
            string motivo,
            CancellationToken cancelacion = default)
        {
            return EnviarOperacionAsync(
                "api/ventas/anular",
                token,
                new AnularVentaApi
                {
                    IdVenta = idVenta,
                    Motivo = motivo
                },
                cancelacion);
        }

        public async Task<ComprobanteVentaApi?> ObtenerComprobanteAsync(
            string? token,
            int idVenta,
            CancellationToken cancelacion = default)
        {
            RespuestaComprobante respuesta =
                await EnviarAsync<RespuestaComprobante>(
                    HttpMethod.Get,
                    $"api/ventas/comprobante/{idVenta}",
                    token,
                    null,
                    cancelacion);

            return respuesta.Comprobante;
        }

        public async Task<byte[]?> ObtenerComprobantePdfAsync(
            string? token,
            int idVenta,
            CancellationToken cancelacion = default)
        {
            using HttpRequestMessage peticion = new(
                HttpMethod.Get,
                $"api/ventas/comprobante/{idVenta}/pdf");

            AplicarToken(peticion, token);

            using HttpResponseMessage respuesta =
                await _http.SendAsync(peticion, cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                string texto = await respuesta.Content.ReadAsStringAsync(cancelacion);

                throw new VentasApiException(
                    (int)respuesta.StatusCode,
                    LeerMensaje(texto)
                        ?? $"La API de ventas respondió con el código {(int)respuesta.StatusCode}.");
            }

            return await respuesta.Content.ReadAsByteArrayAsync(cancelacion);
        }

        // ---------- Infraestructura HTTP ----------

        private async Task<ResultadoVentaApi> EnviarOperacionAsync(
            string ruta,
            string? token,
            object cuerpo,
            CancellationToken cancelacion)
        {
            using HttpRequestMessage peticion = new(HttpMethod.Post, ruta);

            AplicarToken(peticion, token);
            peticion.Content = JsonContent.Create(cuerpo);

            using HttpResponseMessage respuesta =
                await _http.SendAsync(peticion, cancelacion);

            string texto = await respuesta.Content.ReadAsStringAsync(cancelacion);

            if (respuesta.IsSuccessStatusCode || respuesta.StatusCode == HttpStatusCode.BadRequest)
            {
                ResultadoVentaApi? contenido =
                    Deserializar<ResultadoVentaApi>(texto);

                if (contenido is not null)
                {
                    return contenido;
                }
            }

            throw new VentasApiException(
                (int)respuesta.StatusCode,
                LeerMensaje(texto)
                    ?? $"La API de ventas respondió con el código {(int)respuesta.StatusCode}.");
        }

        private async Task<T> EnviarAsync<T>(
            HttpMethod metodo,
            string ruta,
            string? token,
            object? cuerpo,
            CancellationToken cancelacion)
        {
            using HttpRequestMessage peticion = new(metodo, ruta);

            AplicarToken(peticion, token);

            if (cuerpo is not null)
            {
                peticion.Content = JsonContent.Create(cuerpo);
            }

            using HttpResponseMessage respuesta =
                await _http.SendAsync(peticion, cancelacion);

            string texto = await respuesta.Content.ReadAsStringAsync(cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                throw new VentasApiException(
                    (int)respuesta.StatusCode,
                    LeerMensaje(texto)
                        ?? $"La API de ventas respondió con el código {(int)respuesta.StatusCode}.");
            }

            T? contenido = Deserializar<T>(texto);

            if (contenido is null)
            {
                throw new VentasApiException(
                    (int)HttpStatusCode.BadGateway,
                    "La API de ventas devolvió una respuesta vacía.");
            }

            return contenido;
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
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(texto, OpcionesJson);
            }
            catch (JsonException)
            {
                return default;
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

        public sealed class RespuestaClientes
        {
            public List<ClienteBusquedaApi> Clientes { get; set; } = new();
        }

        public sealed class RespuestaProductos
        {
            public List<ProductoBusquedaApi> Productos { get; set; } = new();
        }

        public sealed class RespuestaCatalogo
        {
            public List<ProductoCatalogoApi> Productos { get; set; } = new();
        }

        public sealed class RespuestaVentas
        {
            public List<VentaListadoApi> Ventas { get; set; } = new();
        }

        public sealed class RespuestaComprobante
        {
            public ComprobanteVentaApi? Comprobante { get; set; }
        }
    }
}
