namespace FERRETERIA__Joel.Dominio.Entidades
{
    public class Empleado
    {
        public int IdEmpleado { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;

        // Nombre de usuario generado automÃ¡ticamente a partir del nombre y el
        // apellido (por ejemplo "mlopez"). Es Ãºnico a nivel de base de datos.
        public string? NombreUsuario { get; set; }

        public string Rol { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? CorreoElectronico { get; set; }

        // Nula mientras la cuenta estÃ¡ pendiente de activaciÃ³n.
        public string? Contrasena { get; set; }

        // Token de un solo uso para activar la cuenta desde el correo.
        public string? TokenActivacion { get; set; }

        // Vencimiento del token de activaciÃ³n (10 minutos desde su emisiÃ³n).
        public DateTime? TokenExpira { get; set; }

        // Indica si el token ya fue consumido (evita reutilizar el enlace).
        public bool TokenUsado { get; set; }

        public sbyte Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public int? IdEmpleadoResponsable { get; set; }
    }
}

