using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Activa la cuenta de un empleado a partir del token recibido por correo.
    /// El token es de un solo uso y vence a los 10 minutos; al validarse, el
    /// empleado define su contraseña y la cuenta pasa al estado activo.
    /// </summary>
    public class ServicioActivacionCuenta
    {
        private const string MensajeTokenInvalido =
            "El enlace de activación no es válido o ya fue utilizado.";

        private const string MensajeTokenVencido =
            "El enlace de activación venció. Solicite uno nuevo al administrador.";

        private const string MensajeNoPendiente =
            "Esta cuenta ya fue activada. Puede iniciar sesión.";

        private readonly IRepositoryEmpleado _repositorio;
        private readonly IHasheadorContrasena _hasheador;
        private readonly RegistroCredencialesValidador _validador;

        public ServicioActivacionCuenta(
            IRepositoryEmpleado repositorio,
            IHasheadorContrasena hasheador,
            RegistroCredencialesValidador validador)
        {
            _repositorio = repositorio;
            _hasheador = hasheador;
            _validador = validador;
        }

        /// <summary>
        /// Comprueba el token y devuelve el nombre del empleado si el enlace
        /// sigue siendo válido (para mostrar la pantalla de definir
        /// contraseña).
        /// </summary>
        public async Task<ResultadoActivacion> ValidarTokenAsync(string? token)
        {
            (Empleado? empleado, string? error) =
                await BuscarPorTokenAsync(token);

            if (empleado is null)
            {
                return ResultadoActivacion.Invalido(error!);
            }

            return ResultadoActivacion.Valido(
                $"{empleado.Nombre} {empleado.Apellido}".Trim());
        }

        /// <summary>
        /// Define la contraseña y activa la cuenta. Revalida el token antes de
        /// aplicar los cambios.
        /// </summary>
        public async Task<ResultadoActivacion> ActivarAsync(
            ActivacionSolicitud solicitud)
        {
            (Empleado? empleado, string? error) =
                await BuscarPorTokenAsync(solicitud.Token);

            if (empleado is null)
            {
                return ResultadoActivacion.Invalido(error!);
            }

            Dictionary<string, string> errores =
                _validador.ValidarContrasenaActivacion(
                    solicitud.Contrasena,
                    solicitud.ConfirmarContrasena);

            string nombreEmpleado =
                $"{empleado.Nombre} {empleado.Apellido}".Trim();

            if (errores.Any())
            {
                return new ResultadoActivacion
                {
                    Exitoso = true,
                    NombreEmpleado = nombreEmpleado,
                    ErroresCampo = errores
                };
            }

            await _repositorio.ActivarCuentaAsync(
                empleado.IdEmpleado,
                _hasheador.Hashear(solicitud.Contrasena),
                EstadoEmpleado.Activo);

            return ResultadoActivacion.Valido(nombreEmpleado);
        }

        private async Task<(Empleado? Empleado, string? Error)> BuscarPorTokenAsync(
            string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (null, MensajeTokenInvalido);
            }

            Empleado? empleado =
                await _repositorio.ObtenerPorTokenActivacionAsync(token.Trim());

            if (empleado is null || empleado.TokenUsado)
            {
                return (null, MensajeTokenInvalido);
            }

            if (empleado.Estado == EstadoEmpleado.Activo)
            {
                return (null, MensajeNoPendiente);
            }

            if (empleado.Estado != EstadoEmpleado.PendienteActivacion)
            {
                return (null, MensajeTokenInvalido);
            }

            if (empleado.TokenExpira is null
                || empleado.TokenExpira.Value < DateTime.Now)
            {
                return (null, MensajeTokenVencido);
            }

            return (empleado, null);
        }
    }
}
