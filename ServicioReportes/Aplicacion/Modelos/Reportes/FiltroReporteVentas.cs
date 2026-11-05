namespace ServicioReportes.Aplicacion.Modelos.Reportes
{
    /// <summary>
    /// Filtros validados del listado de productos.
    /// </summary>
    public sealed record FiltroReporteVentas
    {
        public required int StockMinimo { get; init; }

        public required decimal PrecioMaximo { get; init; }

        public string FiltroTexto =>
            $"Stock > {StockMinimo} · Precio < Bs {PrecioMaximo:N2}";
    }
}
