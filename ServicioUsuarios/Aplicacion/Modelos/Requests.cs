namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    public class LoginRequest
    {
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
    }

    public class CrearUsuarioRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class ActualizarUsuarioRequest : CrearUsuarioRequest { }

    public class ActivacionRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }

    public class ValidarActivacionRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class CambioContrasenaRequest
    {
        public string ContrasenaActual { get; set; } = string.Empty;
        public string ContrasenaNueva { get; set; } = string.Empty;
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }

    public class BitacoraRequest
    {
        public string Operacion { get; set; } = string.Empty;
        public string Entidad { get; set; } = string.Empty;
        public int? IdRegistroAfectado { get; set; }
        public bool Exitoso { get; set; } = true;
    }
}

