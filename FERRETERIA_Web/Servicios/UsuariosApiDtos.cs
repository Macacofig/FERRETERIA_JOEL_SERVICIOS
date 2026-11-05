namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// DTOs que el front usa para hablar con ServicioUsuarios. Son una copia
    /// deliberada del contrato HTTP: la capa Razor no comparte entidades ni
    /// repositorios con la API, solo JSON.
    /// </summary>
    public class RespuestaApi<T>
    {
        public bool Exitoso { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public T? Datos { get; set; }

        public List<string> Errores { get; set; } = new();

        public Dictionary<string, string> ErroresCampo { get; set; } = new();
    }

    public class UsuarioApi
    {
        public int IdEmpleado { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string? NombreUsuario { get; set; }

        public string Rol { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string? CorreoElectronico { get; set; }

        public sbyte Estado { get; set; }

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaActualizacion { get; set; }

        public int? IdEmpleadoResponsable { get; set; }

        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    }

    public class LoginApiRespuesta
    {
        public string Token { get; set; } = string.Empty;

        public DateTime Expira { get; set; }

        public UsuarioApi Usuario { get; set; } = new();
    }

    public class CrearUsuarioApi
    {
        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string CorreoElectronico { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;
    }

    public class ActualizarUsuarioApi
    {
        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string CorreoElectronico { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;
    }

    public class CrearUsuarioApiRespuesta
    {
        public int Id { get; set; }

        public string? NombreUsuario { get; set; }

        public string? EnlaceActivacion { get; set; }

        public UsuarioApi? Usuario { get; set; }
    }

    public class ActivacionApiResultado
    {
        public bool Exitoso { get; set; }

        public string NombreEmpleado { get; set; } = string.Empty;

        public string Mensaje { get; set; } = string.Empty;

        public Dictionary<string, string> ErroresCampo { get; set; } = new();
    }

    public class CambioContrasenaApi
    {
        public string ContrasenaActual { get; set; } = string.Empty;

        public string ContrasenaNueva { get; set; } = string.Empty;

        public string ConfirmarContrasena { get; set; } = string.Empty;
    }

    public class CambioContrasenaApiRespuesta
    {
        public string Token { get; set; } = string.Empty;

        public DateTime Expira { get; set; }

        public UsuarioApi? Usuario { get; set; }
    }

    public class RegistroBitacoraApi
    {
        public string Operacion { get; set; } = string.Empty;

        public string Entidad { get; set; } = string.Empty;

        public int? IdRegistroAfectado { get; set; }

        public bool Exitoso { get; set; } = true;
    }

    public class BitacoraApi
    {
        public int IdBitacora { get; set; }

        public string Accion { get; set; } = string.Empty;

        public int? IdUsuario { get; set; }

        public string? NombreUsuario { get; set; }

        public string TipoUsuario { get; set; } = string.Empty;

        public DateTime FechaHora { get; set; }

        public string? Entidad { get; set; }

        public int? IdRegistroAfectado { get; set; }

        public string Resultado { get; set; } = string.Empty;
    }
}
