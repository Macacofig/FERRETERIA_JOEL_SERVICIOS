namespace ServicioReportes.Aplicacion.Modelos.Reportes
{
    /// <summary>
    /// Resultado completo del listado de productos.
    /// </summary>
    public sealed record ReporteVentas
    {
        public required FiltroReporteVentas Filtro { get; init; }

        public required IReadOnlyList<LineaReporteVentas> Lineas { get; init; }

        public required string GeneradoPorCompleto { get; init; }

        public required string GeneradoPor { get; init; }

        public required DateTime GeneradoEl { get; init; }

        public string Titulo => "Productos de stock estable y precio razonable";

        public int TotalRegistros => Lineas.Count;

        public int TotalStock => Lineas.Sum(linea => linea.CantidadStock);

        public decimal PrecioPromedio => Lineas.Count == 0
            ? 0
            : Lineas.Average(linea => linea.Precio);
    }
}
