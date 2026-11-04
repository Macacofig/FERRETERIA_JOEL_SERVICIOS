using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Dominio.Puertos
{
    /// <summary>
    /// Contrato del repositorio de inventario. No encaja en el CRUD genérico:
    /// expone las operaciones de stock que participan de la transacción de
    /// venta o anulación.
    /// </summary>
    public interface IInventarioRepository
    {
        Task<Producto?> ObtenerParaVentaAsync(int idProducto);

        Task<bool> DescontarAsync(int idProducto, int cantidad);

        Task ReponerAsync(int idProducto, int cantidad);

        Task<List<Producto>> ListarActivosAsync();

        Task<List<Producto>> ObtenerPorIdsAsync(IReadOnlyCollection<int> ids);
    }
}
