namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Resultado del cambio de contraseÃ±a. Los errores por campo se muestran
    /// junto a cada entrada del formulario.
    /// </summary>
    public class ResultadoCambioContrasena
    {
        public bool Exitoso { get; init; }

        public string Mensaje { get; init; } = string.Empty;

        public Dictionary<string, string> ErroresCampo { get; init; } = new();

        public static ResultadoCambioContrasena Ok(string mensaje)
        {
            return new ResultadoCambioContrasena
            {
                Exitoso = true,
                Mensaje = mensaje
            };
        }

        public static ResultadoCambioContrasena Error(string mensaje)
        {
            return new ResultadoCambioContrasena
            {
                Exitoso = false,
                Mensaje = mensaje
            };
        }
    }
}

