using FERRETERIA__Joel.Dominio.Entidades;

namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Contrato de persistencia del personal. Extiende el contrato genÃ©rico con
    /// las operaciones propias del ciclo de vida de una cuenta: cambiar su
    /// estado de forma explÃ­cita (baja lÃ³gica o reactivaciÃ³n) y consultar el
    /// personal con proyecciones libres de credenciales.
    ///
    /// <para>
    /// El contrato separa deliberadamente tres tipos de lectura:
    /// <list type="bullet">
    /// <item>las proyecciones <em>sin credenciales</em>, para todo lo que llega
    /// a la interfaz o a un listado;</item>
    /// <item>la proyecciÃ³n <em>con credenciales</em>, reservada al proceso de
    /// autenticaciÃ³n;</item>
    /// <item>la proyecciÃ³n del flujo de activaciÃ³n, con el token pero sin la
    /// contraseÃ±a.</item>
    /// </list>
    /// AsÃ­ el hash de la contraseÃ±a nunca sale del proceso que lo necesita.
    /// </para>
    /// </summary>
    public interface IRepositoryEmpleado : IRepository<Empleado>
    {
        /// <summary>
        /// Fija el estado de la cuenta (baja lÃ³gica o reactivaciÃ³n) sin
        /// borrado fÃ­sico. Devuelve <c>false</c> si la cuenta no existe o si
        /// ya se encuentra en el estado solicitado.
        /// </summary>
        Task<bool> ActualizarEstadoAsync(
            int idEmpleado,
            sbyte nuevoEstado,
            int idEmpleadoResponsable);

        /// <summary>
        /// Devuelve el personal sin la contraseÃ±a ni el token de activaciÃ³n.
        /// Es la proyecciÃ³n que se expone a la interfaz y a los listados.
        /// </summary>
        Task<List<Empleado>> ObtenerTodosSinCredencialesAsync();

        /// <summary>
        /// Devuelve una cuenta por su identificador con la proyecciÃ³n pÃºblica,
        /// sin la contraseÃ±a ni el token de activaciÃ³n. Se usa para revalidar
        /// que la cuenta siga activa en cada peticiÃ³n autenticada (bloqueo de
        /// acceso tras una baja lÃ³gica).
        /// </summary>
        Task<Empleado?> ObtenerPorIdSinCredencialesAsync(int idEmpleado);

        /// <summary>
        /// Devuelve la cuenta asociada a un correo con la proyecciÃ³n pÃºblica.
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
        /// contraseÃ±a. Se reserva para el proceso de autenticaciÃ³n: el valor
        /// nunca debe salir de la capa de aplicaciÃ³n.
        /// </summary>
        Task<Empleado?> ObtenerPorCorreoConCredencialesAsync(string correo);

        /// <summary>
        /// Devuelve la cuenta por correo o por nombre de usuario junto con su
        /// hash de contraseÃ±a. Se reserva para el proceso de autenticaciÃ³n.
        /// </summary>
        Task<Empleado?> ObtenerPorIdentificadorConCredencialesAsync(
            string identificador);

        /// <summary>
        /// Devuelve una cuenta por su identificador incluyendo el hash de la
        /// contraseÃ±a. Se usa para verificar la contraseÃ±a actual en el cambio
        /// de contraseÃ±a.
        /// </summary>
        Task<Empleado?> ObtenerPorIdConCredencialesAsync(int idEmpleado);

        /// <summary>
        /// Indica si un nombre de usuario ya estÃ¡ ocupado. La generaciÃ³n
        /// automÃ¡tica la usa para resolver colisiones antes de insertar.
        /// </summary>
        Task<bool> ExistePorNombreUsuarioAsync(string nombreUsuario);

        /// <summary>
        /// Actualiza los datos personales de la cuenta sin tocar jamÃ¡s la
        /// contraseÃ±a, el token de activaciÃ³n ni el estado. Registra quiÃ©n
        /// realizÃ³ la ediciÃ³n en <c>IdEmpleadoResponsable</c>.
        /// </summary>
        Task<bool> ActualizarDatosAsync(
            Empleado empleado,
            int idEmpleadoResponsable);

        /// <summary>
        /// Cuentas pendientes de activaciÃ³n (Estado -1), con la proyecciÃ³n del
        /// flujo de activaciÃ³n.
        /// </summary>
        Task<List<Empleado>> ObtenerPendientesActivacionAsync();

        /// <summary>
        /// Busca la cuenta propietaria de un token de activaciÃ³n, sin importar
        /// si venciÃ³ o ya se usÃ³: la capa de aplicaciÃ³n decide si es vÃ¡lido.
        /// </summary>
        Task<Empleado?> ObtenerPorTokenActivacionAsync(string token);

        /// <summary>
        /// Reemplaza el token de activaciÃ³n y reinicia su vigencia.
        /// </summary>
        Task ReasignarTokenActivacionAsync(
            int id,
            string token,
            DateTime expira);

        /// <summary>
        /// Activa la cuenta: guarda la contraseÃ±a, marca el token como usado
        /// y fija el estado activo.
        /// </summary>
        Task ActivarCuentaAsync(
            int id,
            string contrasena,
            sbyte estadoActivo);

        /// <summary>
        /// Sustituye el hash de la contraseÃ±a (rehash o cambio).
        /// </summary>
        Task ActualizarContrasenaAsync(int id, string contrasena);
    }
}

