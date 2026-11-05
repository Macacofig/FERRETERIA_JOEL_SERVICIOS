using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Stock
{
    /// <summary>
    /// Caso de uso: obtener un producto con su stock actual para venderlo.
    /// </summary>
    public interface IObtenerProductoParaVentaUseCase
    {
        Task<Producto?> ObtenerParaVentaAsync(int idProducto);
    }

    public class ObtenerProductoParaVentaUseCase
        : IObtenerProductoParaVentaUseCase
    {
        private readonly RepositoryCreator<IInventarioRepository> _creadorInventario;
        private IInventarioRepository? _inventario;

        public ObtenerProductoParaVentaUseCase(
            RepositoryCreator<IInventarioRepository> creadorInventario)
        {
            _creadorInventario = creadorInventario;
        }

        private IInventarioRepository Inventario =>
            _inventario ??= _creadorInventario.CreateRepository();

        public Task<Producto?> ObtenerParaVentaAsync(int idProducto)
        {
            return Inventario.ObtenerParaVentaAsync(idProducto);
        }
    }
}
