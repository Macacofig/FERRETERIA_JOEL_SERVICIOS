using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Ventas
{
    /// <summary>
    /// Caso de uso: listar el catálogo completo de productos, incluidos los
    /// inactivos, para la pantalla de consulta.
    /// </summary>
    public interface IListarProductosUseCase
    {
        Task<List<Producto>> ListarProductosAsync();
    }

    public class ListarProductosUseCase : IListarProductosUseCase
    {
        private readonly RepositoryCreator<IProductoRepository> _creadorProductos;
        private IProductoRepository? _productos;

        public ListarProductosUseCase(
            RepositoryCreator<IProductoRepository> creadorProductos)
        {
            _creadorProductos = creadorProductos;
        }

        private IProductoRepository Productos =>
            _productos ??= _creadorProductos.CreateRepository();

        public Task<List<Producto>> ListarProductosAsync()
        {
            return Productos.ObtenerTodosAsync();
        }
    }
}
