namespace ServicioReportes.Aplicacion.Modelos.Reportes
{
    /// <summary>
    /// Filtros del reporte sumariado de productos vendidos.
    /// </summary>
    public sealed class SolicitudReporteEstadisticas : SolicitudReporteBase
    {
        public int? CantidadMinima { get; set; } = 3;

        public string? MetodoPago { get; set; } = "Efectivo";

        public FiltroReporteEstadisticas CrearFiltro()
        {
            int cantidad = CantidadMinima ?? 3;
            string metodo = string.IsNullOrWhiteSpace(MetodoPago)
                ? "Efectivo"
                : MetodoPago.Trim();

            if (cantidad < 0)
            {
                throw new Excepciones.ReporteValidacionException(
                    "La cantidad mínima no puede ser negativa.");
            }

            return new FiltroReporteEstadisticas
            {
                CantidadMinima = cantidad,
                MetodoPago = metodo
            };
        }
    }
}
