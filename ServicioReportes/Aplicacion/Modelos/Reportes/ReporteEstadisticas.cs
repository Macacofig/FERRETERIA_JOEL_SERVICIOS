namespace ServicioReportes.Aplicacion.Modelos.Reportes
{
    /// <summary>
    /// Resultado del reporte sumariado por producto con gráfico de torta.
    /// </summary>
    public sealed record ReporteEstadisticas
    {
        public required FiltroReporteEstadisticas Filtro { get; init; }

        public required IReadOnlyList<LineaEstadisticaCategoria> Filas { get; init; }

        public required string GeneradoPorCompleto { get; init; }

        public required string GeneradoPor { get; init; }

        public required DateTime GeneradoEl { get; init; }

        public string Titulo => "Productos vendidos por método de pago";

        public int TotalProductos => Filas.Count;

        public int TotalUnidades => Filas.Sum(fila => fila.CantidadTotal);

        public decimal TotalImporte => Filas.Sum(fila => fila.TotalRecaudado);

        public IReadOnlyList<LineaEstadisticaCategoria> FilasOrdenadas =>
            Filas.OrderByDescending(fila => fila.TotalRecaudado)
                .ThenBy(fila => fila.Producto)
                .ToList();
    }
}
