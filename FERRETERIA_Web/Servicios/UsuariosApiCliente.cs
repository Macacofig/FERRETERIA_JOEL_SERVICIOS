using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Cliente HTTP del front hacia ServicioUsuarios. Es el único punto del
    /// proyecto Razor que conoce la URL de la API y el formato de sus
    /// respuestas. El JWT viaja en la cabecera Authorization y se guarda en la
    /// sesión de servidor, nunca en localStorage.
    /// </summary>
    public class UsuariosApiCliente
    {
        public const string ClaveSesionJwt = "UsuariosJwt";

        private static readonly JsonSerializerOptions OpcionesJson =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient _http;

        public UsuariosApiCliente(HttpClient http)
        {
            _http = http;
        }

        public async Task<RespuestaApi<LoginApiRespuesta>> LoginAsync(
            string identificador,
            string contrasena,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<LoginApiRespuesta>(
                HttpMethod.Post,
                "api/usuarios/login",
                null,
                new
                {
                    CorreoElectronico = identificador,
                    Contrasena = contrasena
                },
                cancelacion);
        }

        public async Task<RespuestaApi<object>> LogoutAsync(
            string? token,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<object>(
                HttpMethod.Post,
                "api/usuarios/logout",
                token,
                null,
                cancelacion);
        }

        public async Task<UsuarioApi?> MeAsync(
            string? token,
            CancellationToken cancelacion = default)
        {
            RespuestaApi<UsuarioApi> respuesta = await EnviarAsync<UsuarioApi>(
                HttpMethod.Get,
                "api/usuarios/me",
                token,
                null,
                cancelacion);

            return respuesta.Exitoso ? respuesta.Datos : null;
        }

        public async Task<RespuestaApi<List<UsuarioApi>>> ListarAsync(
            string? token,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<List<UsuarioApi>>(
                HttpMethod.Get,
                "api/usuarios",
                token,
                null,
                cancelacion);
        }

        public async Task<RespuestaApi<List<UsuarioApi>>> PendientesAsync(
            string? token,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<List<UsuarioApi>>(
                HttpMethod.Get,
                "api/usuarios/pendientes",
                token,
                null,
                cancelacion);
        }

        public async Task<RespuestaApi<UsuarioApi>> ObtenerAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<UsuarioApi>(
                HttpMethod.Get,
                $"api/usuarios/{idEmpleado}",
                token,
                null,
                cancelacion);
        }

        public async Task<RespuestaApi<CrearUsuarioApiRespuesta>> CrearAsync(
            string? token,
            CrearUsuarioApi solicitud,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<CrearUsuarioApiRespuesta>(
                HttpMethod.Post,
                "api/usuarios",
                token,
                solicitud,
                cancelacion);
        }

        public async Task<RespuestaApi<UsuarioApi>> ActualizarAsync(
            string? token,
            int idEmpleado,
            ActualizarUsuarioApi solicitud,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<UsuarioApi>(
                HttpMethod.Put,
                $"api/usuarios/{idEmpleado}",
                token,
                solicitud,
                cancelacion);
        }

        public async Task<RespuestaApi<object>> DarDeBajaAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<object>(
                HttpMethod.Delete,
                $"api/usuarios/{idEmpleado}",
                token,
                null,
                cancelacion);
        }

        public async Task<RespuestaApi<object>> ReactivarAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<object>(
                HttpMethod.Post,
                $"api/usuarios/{idEmpleado}/reactivar",
                token,
                null,
                cancelacion);
        }

        public async Task<RespuestaApi<object>> ReenviarActivacionAsync(
            string? token,
            int idEmpleado,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<object>(
                HttpMethod.Post,
                $"api/usuarios/{idEmpleado}/reenviar-activacion",
                token,
                null,
                cancelacion);
        }

        public async Task<RespuestaApi<ActivacionApiResultado>> ValidarActivacionAsync(
            string tokenActivacion,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<ActivacionApiResultado>(
                HttpMethod.Post,
                "api/usuarios/activar/validar",
                null,
                new { Token = tokenActivacion },
                cancelacion);
        }

        public async Task<RespuestaApi<ActivacionApiResultado>> ActivarAsync(
            string tokenActivacion,
            string contrasena,
            string confirmarContrasena,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<ActivacionApiResultado>(
                HttpMethod.Post,
                "api/usuarios/activar",
                null,
                new
                {
                    Token = tokenActivacion,
                    Contrasena = contrasena,
                    ConfirmarContrasena = confirmarContrasena
                },
                cancelacion);
        }

        public async Task<RespuestaApi<CambioContrasenaApiRespuesta>> CambiarContrasenaAsync(
            string? token,
            CambioContrasenaApi solicitud,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<CambioContrasenaApiRespuesta>(
                HttpMethod.Post,
                "api/usuarios/cambiar-contrasena",
                token,
                solicitud,
                cancelacion);
        }

        public async Task<RespuestaApi<object>> RegistrarBitacoraAsync(
            string? token,
            RegistroBitacoraApi solicitud,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<object>(
                HttpMethod.Post,
                "api/usuarios/bitacora",
                token,
                solicitud,
                cancelacion);
        }

        public async Task<RespuestaApi<List<BitacoraApi>>> ListarBitacoraAsync(
            string? token,
            int limite = 100,
            CancellationToken cancelacion = default)
        {
            return await EnviarAsync<List<BitacoraApi>>(
                HttpMethod.Get,
                $"api/usuarios/bitacora?limite={limite}",
                token,
                null,
                cancelacion);
        }

        private async Task<RespuestaApi<T>> EnviarAsync<T>(
            HttpMethod metodo,
            string ruta,
            string? token,
            object? cuerpo,
            CancellationToken cancelacion)
        {
            using HttpRequestMessage peticion = new(metodo, ruta);

            if (!string.IsNullOrWhiteSpace(token))
            {
                peticion.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            if (cuerpo is not null)
            {
                peticion.Content = JsonContent.Create(cuerpo);
            }

            using HttpResponseMessage respuesta =
                await _http.SendAsync(peticion, cancelacion);

            string texto = await respuesta.Content.ReadAsStringAsync(cancelacion);

            if (!string.IsNullOrWhiteSpace(texto))
            {
                try
                {
                    RespuestaApi<T>? contenido =
                        JsonSerializer.Deserialize<RespuestaApi<T>>(
                            texto,
                            OpcionesJson);

                    if (contenido is not null)
                    {
                        return contenido;
                    }
                }
                catch (JsonException)
                {
                    // Se devuelve un error controlado más abajo.
                }
            }

            return new RespuestaApi<T>
            {
                Exitoso = false,
                Mensaje = respuesta.IsSuccessStatusCode
                    ? "La API devolvió una respuesta vacía."
                    : $"La API respondió con el código {(int)respuesta.StatusCode}.",
                Errores = new List<string>
                {
                    $"La API respondió con el código {(int)respuesta.StatusCode}."
                }
            };
        }
    }
}
