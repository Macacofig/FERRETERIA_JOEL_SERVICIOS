namespace ServicioReportes.Aplicacion.Modelos.Reportes
{
    /// <summary>
    /// Fila del listado de productos. Proviene del join producto + marca +
    /// categoria.
    /// </summary>
    public sealed record LineaReporteVentas
    {
        public required string Codigo { get; init; }

        public required string Producto { get; init; }

        public required decimal Precio { get; init; }

        public string Descripcion { get; init; } = string.Empty;

        public required string Marca { get; init; }

        public required string Categoria { get; init; }

        public required int CantidadStock { get; init; }
    }
}
