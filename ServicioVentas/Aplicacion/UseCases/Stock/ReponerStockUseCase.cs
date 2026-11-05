using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Stock
{
    /// <summary>
    /// Caso de uso: reponer stock de un producto dentro de la transacción de
    /// anulación.
    /// </summary>
    public interface IReponerStockUseCase
    {
        Task ReponerAsync(int idProducto, int cantidad);
    }

    public class ReponerStockUseCase : IReponerStockUseCase
    {
        private readonly RepositoryCreator<IInventarioRepository> _creadorInventario;
        private IInventarioRepository? _inventario;

        public ReponerStockUseCase(
            RepositoryCreator<IInventarioRepository> creadorInventario)
        {
            _creadorInventario = creadorInventario;
        }

        private IInventarioRepository Inventario =>
            _inventario ??= _creadorInventario.CreateRepository();

        public Task ReponerAsync(int idProducto, int cantidad)
        {
            return Inventario.ReponerAsync(idProducto, cantidad);
        }
    }
}
