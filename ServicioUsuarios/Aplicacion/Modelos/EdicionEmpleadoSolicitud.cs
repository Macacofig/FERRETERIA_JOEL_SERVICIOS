using FERRETERIA__Joel.Dominio.Constantes;

namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Datos editables de una cuenta. La contraseÃ±a no forma parte de esta
    /// ediciÃ³n: se cambia Ãºnicamente desde el flujo de cambio de contraseÃ±a.
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

