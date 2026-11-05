using ServicioVentas.Aplicacion.Modelos;

namespace ServicioVentas.Aplicacion.UseCases.Ventas
{
    /// <summary>
    /// Caso de uso: calcular los totales de una venta a partir de sus líneas.
    /// </summary>
    public interface ICalcularResumenVentaUseCase
    {
        ResumenVenta CalcularResumen(IEnumerable<LineaVentaDetalle> lineas);
    }

    public class CalcularResumenVentaUseCase : ICalcularResumenVentaUseCase
    {
        public ResumenVenta CalcularResumen(IEnumerable<LineaVentaDetalle> lineas)
        {
            decimal total = lineas.Sum(l => l.TotalProducto);

            return new ResumenVenta
            {
                MontoTotal = Math.Round(total, 2)
            };
        }
    }
}
