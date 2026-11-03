using FERRETERIA__Joel.Dominio.Entidades;

namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Contrato de persistencia del personal. Extiende el contrato genérico con
    /// las operaciones propias del ciclo de vida de una cuenta: cambiar su
    /// estado de forma explícita (baja lógica o reactivación) y consultar el
    /// personal con proyecciones libres de credenciales.
    ///
    /// <para>
    /// El contrato separa deliberadamente tres tipos de lectura:
    /// <list type="bullet">
    /// <item>las proyecciones <em>sin credenciales</em>, para todo lo que llega
    /// a la interfaz o a un listado;</item>
    /// <item>la proyección <em>con credenciales</em>, reservada al proceso de
    /// autenticación;</item>
    /// <item>la proyección del flujo de activación, con el token pero sin la
    /// contraseña.</item>
    /// </list>
    /// Así el hash de la contraseña nunca sale del proceso que lo necesita.
    /// </para>
    /// </summary>
    public interface IRepositoryEmpleado : IRepository<Empleado>
    {
        /// <summary>
        /// Fija el estado de la cuenta (baja lógica o reactivación) sin
        /// borrado físico. Devuelve <c>false</c> si la cuenta no existe o si
        /// ya se encuentra en el estado solicitado.
        /// </summary>
        Task<bool> ActualizarEstadoAsync(
            int idEmpleado,
            sbyte nuevoEstado,
            int idEmpleadoResponsable);

        /// <summary>
        /// Devuelve el personal sin la contraseña ni el token de activación.
        /// Es la proyección que se expone a la interfaz y a los listados.
        /// </summary>
        Task<List<Empleado>> ObtenerTodosSinCredencialesAsync();

        /// <summary>
        /// Devuelve una cuenta por su identificador con la proyección pública,
        /// sin la contraseña ni el token de activación. Se usa para revalidar
        /// que la cuenta siga activa en cada petición autenticada (bloqueo de
        /// acceso tras una baja lógica).
        /// </summary>
        Task<Empleado?> ObtenerPorIdSinCredencialesAsync(int idEmpleado);

        /// <summary>
        /// Devuelve la cuenta asociada a un correo con la proyección pública.
        /// Se usa para comprobar el estado de una cuenta sin exponer
        /// credenciales.
        /// </summary>
        Task<Empleado?> ObtenerPorCorreoSinCredencialesAsync(string correo);

        /// <summary>
        /// Indica si ya existe una cuenta con ese correo. La consulta no
        /// recupera ninguna credencial.
        /// </summary>
        Task<bool> ExistePorCorreoAsync(string correo);

        /// <summary>
        /// Devuelve la cuenta asociada a un correo junto con su hash de
        /// contraseña. Se reserva para el proceso de autenticación: el valor
        /// nunca debe salir de la capa de aplicación.
        /// </summary>
        Task<Empleado?> ObtenerPorCorreoConCredencialesAsync(string correo);

        /// <summary>
        /// Devuelve la cuenta por correo o por nombre de usuario junto con su
        /// hash de contraseña. Se reserva para el proceso de autenticación.
        /// </summary>
        Task<Empleado?> ObtenerPorIdentificadorConCredencialesAsync(
            string identificador);

        /// <summary>
        /// Devuelve una cuenta por su identificador incluyendo el hash de la
        /// contraseña. Se usa para verificar la contraseña actual en el cambio
        /// de contraseña.
        /// </summary>
        Task<Empleado?> ObtenerPorIdConCredencialesAsync(int idEmpleado);

        /// <summary>
        /// Indica si un nombre de usuario ya está ocupado. La generación
        /// automática la usa para resolver colisiones antes de insertar.
        /// </summary>
        Task<bool> ExistePorNombreUsuarioAsync(string nombreUsuario);

        /// <summary>
        /// Actualiza los datos personales de la cuenta sin tocar jamás la
        /// contraseña, el token de activación ni el estado. Registra quién
        /// realizó la edición en <c>IdEmpleadoResponsable</c>.
        /// </summary>
        Task<bool> ActualizarDatosAsync(
            Empleado empleado,
            int idEmpleadoResponsable);

        /// <summary>
        /// Cuentas pendientes de activación (Estado -1), con la proyección del
        /// flujo de activación.
        /// </summary>
        Task<List<Empleado>> ObtenerPendientesActivacionAsync();

        /// <summary>
        /// Busca la cuenta propietaria de un token de activación, sin importar
        /// si venció o ya se usó: la capa de aplicación decide si es válido.
        /// </summary>
        Task<Empleado?> ObtenerPorTokenActivacionAsync(string token);

        /// <summary>
        /// Reemplaza el token de activación y reinicia su vigencia.
        /// </summary>
        Task ReasignarTokenActivacionAsync(
            int id,
            string token,
            DateTime expira);

        /// <summary>
        /// Activa la cuenta: guarda la contraseña, marca el token como usado
        /// y fija el estado activo.
        /// </summary>
        Task ActivarCuentaAsync(
            int id,
            string contrasena,
            sbyte estadoActivo);

        /// <summary>
        /// Sustituye el hash de la contraseña (rehash o cambio).
        /// </summary>
        Task ActualizarContrasenaAsync(int id, string contrasena);
    }
}
