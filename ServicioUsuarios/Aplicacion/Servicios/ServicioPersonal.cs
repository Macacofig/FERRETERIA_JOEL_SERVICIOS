using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Gestión del personal: consulta del listado y del ciclo de vida de las
    /// cuentas (baja lógica y reactivación).
    ///
    /// <para>
    /// La baja es <em>lógica</em>: la fila no se borra nunca, se cambia su
    /// estado a <see cref="EstadoEmpleado.Inactivo"/>. De ese modo el
    /// histórico de ventas, los comprobantes y la auditoría siguen siendo
    /// trazables, y la cuenta deja de poder iniciar sesión porque el proceso
    /// de autenticación exige el estado activo.
    /// </para>
    /// </summary>
    public class ServicioPersonal
    {
        private readonly IRepositoryEmpleado _repositorio;
        private readonly IContextoUsuario _contextoUsuario;
        private readonly ServicioRegistroEmpleado _servicioActivacion;

        public ServicioPersonal(
            IRepositoryEmpleado repositorio,
            IContextoUsuario contextoUsuario,
            ServicioRegistroEmpleado servicioActivacion)
        {
            _repositorio = repositorio;
            _contextoUsuario = contextoUsuario;
            _servicioActivacion = servicioActivacion;
        }

        /// <summary>
        /// Lista el personal con la proyección sin credenciales: la contraseña
        /// y el token de activación nunca llegan a la interfaz.
        /// </summary>
        public async Task<List<Empleado>> ListarAsync()
        {
            return await _repositorio.ObtenerTodosSinCredencialesAsync();
        }

        /// <summary>
        /// Reenvía el enlace de activación a una cuenta pendiente (Estado -1):
        /// genera un token nuevo y vuelve a enviar el correo. Se usa cuando el
        /// enlace anterior venció o se perdió.
        /// </summary>
        public async Task<ResultadoPersonal> ReenviarActivacionAsync(
            int idEmpleado)
        {
            if (idEmpleado <= 0)
            {
                return new ResultadoPersonal
                {
                    Exitoso = false,
                    Mensaje = "El empleado indicado no es válido."
                };
            }

            ResultadoRegistro resultado =
                await _servicioActivacion.ReenviarActivacionAsync(idEmpleado);

            if (!resultado.Exitoso)
            {
                return new ResultadoPersonal
                {
                    Exitoso = false,
                    IdEmpleado = idEmpleado,
                    Mensaje = resultado.Errores.FirstOrDefault()
                        ?? "No se pudo reenviar el enlace de activación."
                };
            }

            return new ResultadoPersonal
            {
                Exitoso = true,
                IdEmpleado = idEmpleado,
                Estado = EstadoEmpleado.PendienteActivacion,
                Mensaje =
                    "Se reenvió el enlace de activación al correo del empleado."
            };
        }

        /// <summary>
        /// Da de baja a un empleado: la cuenta pasa a estado inactivo sin
        /// borrado físico y deja de poder iniciar sesión.
        /// </summary>
        public async Task<ResultadoPersonal> DarDeBajaAsync(int idEmpleado)
        {
            return await CambiarEstadoAsync(
                idEmpleado,
                EstadoEmpleado.Inactivo,
                "La cuenta fue dada de baja.");
        }

        /// <summary>
        /// Reactiva una cuenta dada de baja. La cuenta vuelve a estado
        /// pendiente de activación y se le reenvía el enlace de activación al
        /// correo: el empleado debe definir de nuevo su contraseña.
        /// </summary>
        public async Task<ResultadoPersonal> ReactivarAsync(int idEmpleado)
        {
            ResultadoPersonal resultado = await CambiarEstadoAsync(
                idEmpleado,
                EstadoEmpleado.PendienteActivacion,
                "La cuenta fue reactivada y quedó pendiente de activación.");

            if (!resultado.Exitoso)
            {
                return resultado;
            }

            // Se reenvía el enlace de activación con un token nuevo para que el
            // empleado pueda definir su contraseña de nuevo.
            try
            {
                await _servicioActivacion.ReenviarActivacionAsync(idEmpleado);
            }
            catch
            {
                // La reactivación ya se aplicó; si el correo falla, el
                // administrador puede reenviarlo desde la lista de pendientes.
            }

            return resultado;
        }

        private async Task<ResultadoPersonal> CambiarEstadoAsync(
            int idEmpleado,
            sbyte nuevoEstado,
            string mensajeExito)
        {
            if (idEmpleado <= 0)
            {
                return new ResultadoPersonal
                {
                    Exitoso = false,
                    Mensaje = "El empleado indicado no es válido."
                };
            }

            if (idEmpleado == _contextoUsuario.IdEmpleado)
            {
                return new ResultadoPersonal
                {
                    Exitoso = false,
                    Mensaje =
                        "No puede cambiar el estado de su propia cuenta."
                };
            }

            bool aplicado = await _repositorio.ActualizarEstadoAsync(
                idEmpleado,
                nuevoEstado,
                _contextoUsuario.IdEmpleado);

            if (!aplicado)
            {
                return new ResultadoPersonal
                {
                    Exitoso = false,
                    IdEmpleado = idEmpleado,
                    Mensaje =
                        "El empleado no existe o ya se encuentra en ese estado."
                };
            }

            return new ResultadoPersonal
            {
                Exitoso = true,
                IdEmpleado = idEmpleado,
                Estado = nuevoEstado,
                Mensaje = mensajeExito
            };
        }
    }
}
