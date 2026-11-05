namespace ServicioReportes.Dominio.Puertos;

public interface IReporteVentasRepository
{
    Task<IReadOnlyList<ProductoEstable>> ObtenerProductosEstablesAsync(
        ReporteProductoFiltros filtros, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VentaProducto>> ObtenerVentasPorProductoAsync(
        ReporteVentaProductoFiltros filtros, CancellationToken cancellationToken = default);
}

public sealed record ReporteProductoFiltros(
    decimal? PrecioMinimo, decimal? PrecioMaximo, int? CantidadVendidaMinima);
public sealed record ProductoEstable(
    string Codigo, string Nombre, decimal CostoUnitario, string Descripcion,
    string Marca, string Categoria);
public sealed record ReporteVentaProductoFiltros(DateTime Desde, DateTime Hasta, string MetodoPago);
public sealed record VentaProducto(string Nombre, int Cantidad, decimal TotalProducto);
