using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.Servicios
{
    /// <summary>
    /// Fachada (Facade) del subsistema de inventario/stock. Aísla las
    /// operaciones de stock para que <see cref="FachadaVenta"/> y
    /// <see cref="FachadaAnulacion"/> no conozcan el repositorio de
    /// inventario ni el SQL que hay detrás.
    ///
    /// No la invoca el código de UI: la usan las otras fachadas. Todas las
    /// operaciones son asíncronas y participan de la transacción ambiente
    /// abierta por la fachada de nivel superior (venta o anulación).
    /// </summary>
    public class FachadaActualizacionStock
    {
        private readonly RepositoryCreator<IInventarioRepository> _creadorInventario;
        private IInventarioRepository? _inventario;

        public FachadaActualizacionStock(
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

        public Task<Producto?> ObtenerParaVentaAsync(int idProducto)
        {
            return Inventario.ObtenerParaVentaAsync(idProducto);
        }

        public Task<List<Producto>> ObtenerPorIdsAsync(
            IReadOnlyCollection<int> ids)
        {
            return Inventario.ObtenerPorIdsAsync(ids);
        }

        /// <summary>
        /// Descuenta stock solo si hay existencias suficientes.
        /// </summary>
        public Task<bool> DescontarAsync(int idProducto, int cantidad)
        {
            return Inventario.DescontarAsync(idProducto, cantidad);
        }

        /// <summary>
        /// Restaura (repone) stock. Usado por la anulación de la venta a
        /// partir de las líneas de detalle_venta.
        /// </summary>
        public Task ReponerAsync(int idProducto, int cantidad)
        {
            return Inventario.ReponerAsync(idProducto, cantidad);
        }

        /// <summary>
        /// Traduce las líneas persistidas de detalle_venta a líneas de
        /// presentación, resolviendo código, nombre y unidad del producto.
        /// </summary>
        public async Task<List<LineaVentaDetalle>> MapearLineasAsync(
            IEnumerable<DetalleVenta> detalles)
        {
            List<DetalleVenta> lista = detalles.ToList();

            List<Producto> productos = await ObtenerPorIdsAsync(
                lista.Select(d => d.IdProducto).Distinct().ToList());

            Dictionary<int, Producto> productoPorId =
                productos.ToDictionary(p => p.IdProducto);

            return lista
                .Select(d =>
                {
                    Producto? producto = productoPorId.GetValueOrDefault(
                        d.IdProducto);

                    return new LineaVentaDetalle
                    {
                        IdProducto = d.IdProducto,
                        Codigo = producto?.Codigo ?? string.Empty,
                        Nombre = producto?.Nombre
                            ?? $"Producto #{d.IdProducto}",
                        UnidadMedida = producto?.UnidadMedida ?? string.Empty,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        TotalProducto = d.TotalProducto
                    };
                })
                .ToList();
        }
    }
}
