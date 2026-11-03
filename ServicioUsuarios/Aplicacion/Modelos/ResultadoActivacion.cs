namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Resultado de una operación de activación de cuenta: indica si el token
    /// es válido y, si no, el mensaje a mostrar y los errores por campo.
    /// </summary>
    public class ResultadoActivacion
    {
        public bool Exitoso { get; init; }

        /// <summary>Nombre completo para saludar en la pantalla de activación.</summary>
        public string NombreEmpleado { get; init; } = string.Empty;

        public string Mensaje { get; init; } = string.Empty;

        public Dictionary<string, string> ErroresCampo { get; init; } = new();

        public static ResultadoActivacion Valido(string nombreEmpleado)
        {
            return new ResultadoActivacion
            {
                Exitoso = true,
                NombreEmpleado = nombreEmpleado
            };
        }

        public static ResultadoActivacion Invalido(string mensaje)
        {
            return new ResultadoActivacion
            {
                Exitoso = false,
                Mensaje = mensaje
            };
        }
    }
}
