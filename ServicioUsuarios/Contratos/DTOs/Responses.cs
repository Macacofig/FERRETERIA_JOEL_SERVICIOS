namespace FERRETERIA__Joel.ServicioUsuarios.Contratos
{
    public class RespuestaApi<T>
    {
        public bool Exitoso { get; init; }
        public string Mensaje { get; init; } = string.Empty;
        public T? Datos { get; init; }
        public List<string> Errores { get; init; } = new();
        public Dictionary<string, string> ErroresCampo { get; init; } = new();

        public static RespuestaApi<T> Ok(T datos, string mensaje = "") => new()
        {
            Exitoso = true, Mensaje = mensaje, Datos = datos
        };

        public static RespuestaApi<T> Error(string mensaje) => new()
        {
            Exitoso = false, Mensaje = mensaje,
            Errores = new List<string> { mensaje }
        };

        public static RespuestaApi<T> ConErrores(
            Dictionary<string, string> erroresCampo,
            string mensaje) => new()
        {
            Exitoso = false, Mensaje = mensaje,
            Errores = new List<string> { mensaje },
            ErroresCampo = erroresCampo ?? new()
        };
    }

    /// <summary>Proyección pública sin contraseña, hash ni token.</summary>
    public class UsuarioDto
    {
        public int IdEmpleado { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string Apellido { get; init; } = string.Empty;
        public string? NombreUsuario { get; init; }
        public string Rol { get; init; } = string.Empty;
        public string Telefono { get; init; } = string.Empty;
        public string? CorreoElectronico { get; init; }
        public sbyte Estado { get; init; }
        public DateTime FechaRegistro { get; init; }
        public DateTime? FechaActualizacion { get; init; }
        public int? IdEmpleadoResponsable { get; init; }
    }

    public class LoginResponse
    {
        public string Token { get; init; } = string.Empty;
        public DateTime Expira { get; init; }
        public UsuarioDto Usuario { get; init; } = new();
    }
}
