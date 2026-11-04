namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Resultado del registro: indica si fue exitoso y, si no, los mensajes
    /// generales y por campo para mostrarlos en el formulario.
    /// </summary>
    public class ResultadoRegistro
    {
        public bool Exitoso { get; init; }

        public int Id { get; init; }

        /// <summary>Nombre de usuario generado automáticamente en el alta.</summary>
        public string? NombreUsuario { get; init; }

        /// <summary>
        /// Enlace de activación generado (solo se rellena en desarrollo, para
        /// poder probar el flujo cuando no hay SMTP configurado).
        /// </summary>
        public string? EnlaceActivacion { get; init; }

        public List<string> Errores { get; init; } = new();

        public Dictionary<string, string> ErroresCampo { get; init; } = new();
    }
}

