using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.UseCases.Stock;
using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Aplicacion.UseCases.Ventas
{
    /// <summary>
    /// Caso de uso: calcular una línea de venta validando cantidad, estado y
    /// stock del producto. El precio nunca llega desde la presentación.
    /// </summary>
    public interface ICalcularLineaVentaUseCase
    {
        Task<(LineaVentaDetalle? Linea, string? Error)> CalcularLineaAsync(
            int idProducto,
            int cantidad);
    }

    public class CalcularLineaVentaUseCase : ICalcularLineaVentaUseCase
    {
        private readonly IObtenerProductoParaVentaUseCase _obtenerProducto;

        public CalcularLineaVentaUseCase(
            IObtenerProductoParaVentaUseCase obtenerProducto)
        {
            _obtenerProducto = obtenerProducto;
        }

        public async Task<(LineaVentaDetalle? Linea, string? Error)> CalcularLineaAsync(
            int idProducto,
            int cantidad)
        {
            if (cantidad <= 0)
            {
                return (null, "La cantidad debe ser mayor que cero.");
            }

            Producto? producto =
                await _obtenerProducto.ObtenerParaVentaAsync(idProducto);

            if (producto is null || producto.Estado != 1)
            {
                return (null,
                    "El producto seleccionado no existe o está inactivo.");
            }

            if (cantidad > producto.CantidadStock)
            {
                return (null,
                    $"Stock insuficiente para '{producto.Nombre}'. " +
                    $"Disponible: {producto.CantidadStock}.");
            }

            return (new LineaVentaDetalle
            {
                IdProducto = producto.IdProducto,
                Codigo = producto.Codigo,
                Nombre = producto.Nombre,
                UnidadMedida = producto.UnidadMedida,
                Cantidad = cantidad,
                PrecioUnitario = producto.PrecioVenta,
                TotalProducto = Math.Round(producto.PrecioVenta * cantidad, 2),
                CantidadStock = producto.CantidadStock
            }, null);
        }
    }
}
