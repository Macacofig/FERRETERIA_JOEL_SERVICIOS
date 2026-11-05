using ServicioReportes.Aplicacion.Modelos.Reportes;

namespace ServicioReportes.Aplicacion.Puertos
{
    /// <summary>
    /// Contrato de persistencia de los reportes. La capa de aplicación solo
    /// conoce este puerto; la implementación concreta (ADO.NET/MySQL) vive en
    /// Infraestructura y se inyecta por DI.
    /// </summary>
    public interface IReporteRepository
    {
        Task<IReadOnlyList<LineaReporteVentas>> ObtenerVentasAsync(
            FiltroReporteVentas filtro,
            CancellationToken cancelacion = default);

        Task<IReadOnlyList<LineaEstadisticaCategoria>> ObtenerEstadisticasAsync(
            FiltroReporteEstadisticas filtro,
            CancellationToken cancelacion = default);

        Task<IReadOnlyList<CategoriaOpcion>> ObtenerCategoriasAsync(
            CancellationToken cancelacion = default);

        Task<string?> ObtenerNombreCategoriaAsync(
            int idCategoria,
            CancellationToken cancelacion = default);
    }
}
