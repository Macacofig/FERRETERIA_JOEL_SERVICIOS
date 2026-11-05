namespace ServicioReportes.Aplicacion.Excepciones
{
    /// <summary>
    /// Error de validación de los filtros del reporte. La API lo traduce a un
    /// HTTP 400 con un mensaje apto para mostrar al usuario.
    /// </summary>
    public sealed class ReporteValidacionException : Exception
    {
        public ReporteValidacionException(string mensaje)
            : base(mensaje)
        {
        }
    }
}
