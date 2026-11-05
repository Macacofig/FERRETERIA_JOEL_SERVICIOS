using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.UseCases.Stock;
using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Aplicacion.Servicios
{
    /// <summary>
    /// Fachada (Facade) del subsistema de inventario/stock. Implementa los
    /// casos de uso de stock y delega en ellos, de modo que las fachadas de
    /// venta y anulación solo dependan de los contratos de este subsistema y
    /// no conozcan repositorios ni SQL.
    ///
    /// No la invoca el código de UI: la usan las fachadas de nivel superior y
    /// los casos de uso de ventas. Todas las operaciones son asíncronas y
    /// participan de la transacción ambiente abierta por la fachada de nivel
    /// superior (venta o anulación).
    /// </summary>
    public class FachadaActualizacionStock :
        IListarProductosActivosUseCase,
        IObtenerProductoParaVentaUseCase,
        IObtenerProductosPorIdsUseCase,
        IDescontarStockUseCase,
        IReponerStockUseCase,
        IMapearLineasVentaUseCase
    {
        private readonly ListarProductosActivosUseCase _listarActivos;
        private readonly ObtenerProductoParaVentaUseCase _obtenerParaVenta;
        private readonly ObtenerProductosPorIdsUseCase _obtenerPorIds;
        private readonly DescontarStockUseCase _descontar;
        private readonly ReponerStockUseCase _reponer;
        private readonly MapearLineasVentaUseCase _mapearLineas;

        public FachadaActualizacionStock(
            ListarProductosActivosUseCase listarActivos,
            ObtenerProductoParaVentaUseCase obtenerParaVenta,
            ObtenerProductosPorIdsUseCase obtenerPorIds,
            DescontarStockUseCase descontar,
            ReponerStockUseCase reponer,
            MapearLineasVentaUseCase mapearLineas)
        {
            _listarActivos = listarActivos;
            _obtenerParaVenta = obtenerParaVenta;
            _obtenerPorIds = obtenerPorIds;
            _descontar = descontar;
            _reponer = reponer;
            _mapearLineas = mapearLineas;
        }

        public Task<List<Producto>> ListarActivosAsync()
        {
            return _listarActivos.ListarActivosAsync();
        }

        public Task<Producto?> ObtenerParaVentaAsync(int idProducto)
        {
            return _obtenerParaVenta.ObtenerParaVentaAsync(idProducto);
        }

        public Task<List<Producto>> ObtenerPorIdsAsync(
            IReadOnlyCollection<int> ids)
        {
            return _obtenerPorIds.ObtenerPorIdsAsync(ids);
        }

        public Task<bool> DescontarAsync(int idProducto, int cantidad)
        {
            return _descontar.DescontarAsync(idProducto, cantidad);
        }

        public Task ReponerAsync(int idProducto, int cantidad)
        {
            return _reponer.ReponerAsync(idProducto, cantidad);
        }

        public Task<List<LineaVentaDetalle>> MapearLineasAsync(
            IEnumerable<DetalleVenta> detalles)
        {
            return _mapearLineas.MapearLineasAsync(detalles);
        }
    }
}
