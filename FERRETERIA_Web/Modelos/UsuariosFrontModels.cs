namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    public class InicioSesionSolicitud
    {
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
    }

    public class RegistroSolicitud
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class ActivacionSolicitud
    {
        public string Token { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}
