using ServicioVentas.Aplicacion.UseCases.Stock;
using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Aplicacion.UseCases.Ventas
{
    /// <summary>
    /// Caso de uso: obtener un producto vendible por su identificador.
    /// </summary>
    public interface IObtenerProductoUseCase
    {
        Task<Producto?> ObtenerProductoAsync(int idProducto);
    }

    public class ObtenerProductoUseCase : IObtenerProductoUseCase
    {
        private readonly IObtenerProductoParaVentaUseCase _obtenerParaVenta;

        public ObtenerProductoUseCase(
            IObtenerProductoParaVentaUseCase obtenerParaVenta)
        {
            _obtenerParaVenta = obtenerParaVenta;
        }

        public Task<Producto?> ObtenerProductoAsync(int idProducto)
        {
            return _obtenerParaVenta.ObtenerParaVentaAsync(idProducto);
        }
    }
}
