namespace ServicioReportes.Aplicacion.Modelos.Reportes
{
    /// <summary>
    /// Fila agregada por producto. <see cref="Participacion"/> se calcula en
    /// la capa de aplicación porque depende del total recaudado.
    /// </summary>
    public sealed record LineaEstadisticaCategoria
    {
        public required string Producto { get; init; }

        public required int CantidadTotal { get; init; }

        public required decimal TotalRecaudado { get; init; }

        public decimal Participacion { get; init; }
    }
}
