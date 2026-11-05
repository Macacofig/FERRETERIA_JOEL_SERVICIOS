namespace ServicioReportes.Aplicacion.Modelos.Reportes
{
    /// <summary>
    /// Filtros del reporte de productos con stock estable y precio razonable.
    /// </summary>
    public sealed class SolicitudReporteVentas : SolicitudReporteBase
    {
        public int? StockMinimo { get; set; } = 10;

        public decimal? PrecioMaximo { get; set; } = 500m;

        public FiltroReporteVentas CrearFiltro()
        {
            int stock = StockMinimo ?? 10;
            decimal precio = PrecioMaximo ?? 500m;

            if (stock < 0)
            {
                throw new Excepciones.ReporteValidacionException(
                    "El stock mínimo no puede ser negativo.");
            }

            if (precio <= 0)
            {
                throw new Excepciones.ReporteValidacionException(
                    "El precio máximo debe ser mayor que cero.");
            }

            return new FiltroReporteVentas
            {
                StockMinimo = stock,
                PrecioMaximo = precio
            };
        }
    }
}
