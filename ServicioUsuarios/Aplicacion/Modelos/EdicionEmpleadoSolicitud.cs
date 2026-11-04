using FERRETERIA__Joel.Dominio.Constantes;

namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Datos editables de una cuenta. La contraseña no forma parte de esta
    /// edición: se cambia únicamente desde el flujo de cambio de contraseña.
    /// </summary>
    public class EdicionEmpleadoSolicitud : IRegistroCredenciales
    {
        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string CorreoElectronico { get; set; } = string.Empty;

        public string Rol { get; set; } = RolesSistema.EmpleadoVenta;
    }
}

