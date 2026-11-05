using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Stock
{
    /// <summary>
    /// Caso de uso: descontar stock de un producto dentro de la transacción
    /// de venta.
    /// </summary>
    public interface IDescontarStockUseCase
    {
        Task<bool> DescontarAsync(int idProducto, int cantidad);
    }

    public class DescontarStockUseCase : IDescontarStockUseCase
    {
        private readonly RepositoryCreator<IInventarioRepository> _creadorInventario;
        private IInventarioRepository? _inventario;

        public DescontarStockUseCase(
            RepositoryCreator<IInventarioRepository> creadorInventario)
        {
            _creadorInventario = creadorInventario;
        }

        private IInventarioRepository Inventario =>
            _inventario ??= _creadorInventario.CreateRepository();

        public Task<bool> DescontarAsync(int idProducto, int cantidad)
        {
            return Inventario.DescontarAsync(idProducto, cantidad);
        }
    }
}
