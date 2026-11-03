namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// Adaptador de auditoría para los módulos CRUD del front. Toma el JWT de
    /// la sesión de servidor y delega el registro en ServicioUsuarios, de modo
    /// que el responsable siempre es el usuario autenticado y no un dato del
    /// formulario. Un fallo de auditoría no debe romper la operación de
    /// negocio: se registra en el log de la aplicación.
    /// </summary>
    public class AuditoriaApi
    {
        private readonly UsuariosApiCliente _api;
        private readonly IHttpContextAccessor _accesor;
        private readonly ILogger<AuditoriaApi> _logger;

        public AuditoriaApi(
            UsuariosApiCliente api,
            IHttpContextAccessor accesor,
            ILogger<AuditoriaApi> logger)
        {
            _api = api;
            _accesor = accesor;
            _logger = logger;
        }

        public async Task RegistrarAsync(
            string operacion,
            string entidad,
            int? idRegistroAfectado,
            bool exitoso)
        {
            string? token = _accesor.HttpContext?.Session
                .GetString(UsuariosApiCliente.ClaveSesionJwt);

            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            try
            {
                RespuestaApi<object> respuesta =
                    await _api.RegistrarBitacoraAsync(
                        token,
                        new RegistroBitacoraApi
                        {
                            Operacion = operacion,
                            Entidad = entidad,
                            IdRegistroAfectado = idRegistroAfectado,
                            Exitoso = exitoso
                        });

                if (!respuesta.Exitoso)
                {
                    _logger.LogWarning(
                        "La bitácora rechazó {Operacion} sobre {Entidad}: {Mensaje}",
                        operacion,
                        entidad,
                        respuesta.Mensaje);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "No se pudo enviar {Operacion} sobre {Entidad} a la bitácora.",
                    operacion,
                    entidad);
            }
        }
    }
}
