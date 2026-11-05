using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.UseCases.Stock;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Ventas
{
    /// <summary>
    /// Caso de uso: consultar el detalle de una venta por su identificador.
    /// </summary>
    public interface IObtenerVentaUseCase
    {
        Task<ResultadoVenta?> ObtenerVentaAsync(int idVenta);
    }

    public class ObtenerVentaUseCase : IObtenerVentaUseCase
    {
        private readonly RepositoryCreator<IVentaRepository> _creadorVentas;
        private readonly RepositoryCreator<IDetalleVentaRepository> _creadorDetalles;
        private readonly IMapearLineasVentaUseCase _mapearLineas;

        private IVentaRepository? _ventas;
        private IDetalleVentaRepository? _detalles;

        public ObtenerVentaUseCase(
            RepositoryCreator<IVentaRepository> creadorVentas,
            RepositoryCreator<IDetalleVentaRepository> creadorDetalles,
            IMapearLineasVentaUseCase mapearLineas)
        {
            _creadorVentas = creadorVentas;
            _creadorDetalles = creadorDetalles;
            _mapearLineas = mapearLineas;
        }

        private IVentaRepository Ventas =>
            _ventas ??= _creadorVentas.CreateRepository();

        private IDetalleVentaRepository Detalles =>
            _detalles ??= _creadorDetalles.CreateRepository();

        public async Task<ResultadoVenta?> ObtenerVentaAsync(int idVenta)
        {
            Venta? venta = await Ventas.ObtenerPorIdAsync(idVenta);

            if (venta is null)
            {
                return null;
            }

            return ResultadoVenta.Correcto(
                venta,
                await _mapearLineas.MapearLineasAsync(
                    await Detalles.ObtenerPorVentaAsync(idVenta)));
        }
    }
}
