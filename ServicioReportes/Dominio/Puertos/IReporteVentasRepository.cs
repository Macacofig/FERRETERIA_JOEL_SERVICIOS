namespace ServicioReportes.Dominio.Puertos;

public interface IReporteVentasRepository
{
    Task<IReadOnlyList<ReporteVentaDetalle>> ObtenerDetalleAsync(
        DateTime desde, DateTime hasta, int? empleadoId, string? estado, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReporteVentaResumen>> ObtenerResumenAsync(
        DateTime desde, DateTime hasta, int? categoriaId, CancellationToken cancellationToken = default);
}

public sealed record ReporteVentaDetalle(
    int IdVenta, string Codigo, DateTime Fecha, string Cliente, string Empleado,
    string Estado, decimal MontoTotal);

public sealed record ReporteVentaResumen(
    string Categoria, int CantidadVendida, decimal TotalVentas);
