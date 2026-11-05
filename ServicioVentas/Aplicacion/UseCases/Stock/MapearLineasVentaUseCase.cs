using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Aplicacion.UseCases.Stock
{
    /// <summary>
    /// Caso de uso: convertir las líneas persistidas de detalle_venta en
    /// líneas de presentación, resolviendo código, nombre y unidad.
    /// </summary>
    public interface IMapearLineasVentaUseCase
    {
        Task<List<LineaVentaDetalle>> MapearLineasAsync(
            IEnumerable<DetalleVenta> detalles);
    }

    public class MapearLineasVentaUseCase : IMapearLineasVentaUseCase
    {
        private readonly ObtenerProductosPorIdsUseCase _obtenerProductos;

        public MapearLineasVentaUseCase(
            ObtenerProductosPorIdsUseCase obtenerProductos)
        {
            _obtenerProductos = obtenerProductos;
        }

        public async Task<List<LineaVentaDetalle>> MapearLineasAsync(
            IEnumerable<DetalleVenta> detalles)
        {
            List<DetalleVenta> lista = detalles.ToList();

            List<Producto> productos = await _obtenerProductos.ObtenerPorIdsAsync(
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
