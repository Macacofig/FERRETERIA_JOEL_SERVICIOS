using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Stock
{
    /// <summary>
    /// Caso de uso: resolver varios productos a la vez a partir de sus ids.
    /// </summary>
    public interface IObtenerProductosPorIdsUseCase
    {
        Task<List<Producto>> ObtenerPorIdsAsync(
            IReadOnlyCollection<int> ids);
    }

    public class ObtenerProductosPorIdsUseCase
        : IObtenerProductosPorIdsUseCase
    {
        private readonly RepositoryCreator<IInventarioRepository> _creadorInventario;
        private IInventarioRepository? _inventario;

        public ObtenerProductosPorIdsUseCase(
            RepositoryCreator<IInventarioRepository> creadorInventario)
        {
            _creadorInventario = creadorInventario;
        }

        private IInventarioRepository Inventario =>
            _inventario ??= _creadorInventario.CreateRepository();

        public Task<List<Producto>> ObtenerPorIdsAsync(
            IReadOnlyCollection<int> ids)
        {
            return Inventario.ObtenerPorIdsAsync(ids);
        }
    }
}
