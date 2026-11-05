using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Stock
{
    /// <summary>
    /// Caso de uso: listar los productos activos que pueden venderse.
    /// </summary>
    public interface IListarProductosActivosUseCase
    {
        Task<List<Producto>> ListarActivosAsync();
    }

    public class ListarProductosActivosUseCase : IListarProductosActivosUseCase
    {
        private readonly RepositoryCreator<IInventarioRepository> _creadorInventario;
        private IInventarioRepository? _inventario;

        public ListarProductosActivosUseCase(
            RepositoryCreator<IInventarioRepository> creadorInventario)
        {
            _creadorInventario = creadorInventario;
        }

        private IInventarioRepository Inventario =>
            _inventario ??= _creadorInventario.CreateRepository();

        public Task<List<Producto>> ListarActivosAsync()
        {
            return Inventario.ListarActivosAsync();
        }
    }
}
