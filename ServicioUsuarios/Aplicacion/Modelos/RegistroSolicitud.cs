using FERRETERIA__Joel.Dominio.Constantes;

namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Datos del formulario de alta de empleado. El alta la realiza un
    /// administrador y puede crear cuentas con rol <c>Administrador</c> o
    /// <c>EmpleadoVenta</c>. La contraseña se define después, mediante el
    /// enlace de activación enviado al correo.
    /// </summary>
    public class RegistroSolicitud : IRegistroCredenciales
    {
        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string CorreoElectronico { get; set; } = string.Empty;

        /// <summary>Rol asignado por el administrador (por defecto EmpleadoVenta).</summary>
        public string Rol { get; set; } = RolesSistema.EmpleadoVenta;
    }
}
