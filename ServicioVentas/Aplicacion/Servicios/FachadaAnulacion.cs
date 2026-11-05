using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.UseCases.Ventas;

namespace ServicioVentas.Aplicacion.Servicios
{
    /// <summary>
    /// Fachada (Facade) de las ventas YA REGISTRADAS: consulta del historial,
    /// detalle de una venta y anulación (borrado lógico). Delegando en los
    /// casos de uso correspondientes, la pantalla de Historial no conoce
    /// repositorios, transacciones ni el subsistema de stock.
    /// </summary>
    public class FachadaAnulacion :
        IListarVentasUseCase,
        IObtenerVentaUseCase,
        IAnularVentaUseCase
    {
        private readonly IListarVentasUseCase _listarVentas;
        private readonly IObtenerVentaUseCase _obtenerVenta;
        private readonly IAnularVentaUseCase _anularVenta;

        public FachadaAnulacion(
            IListarVentasUseCase listarVentas,
            IObtenerVentaUseCase obtenerVenta,
            IAnularVentaUseCase anularVenta)
        {
            _listarVentas = listarVentas;
            _obtenerVenta = obtenerVenta;
            _anularVenta = anularVenta;
        }

        // ---------- Consulta (Select / historial) ----------

        public Task<List<VentaListado>> ListarVentasListadoAsync()
        {
            return _listarVentas.ListarVentasListadoAsync();
        }

        public Task<ResultadoVenta?> ObtenerVentaAsync(int idVenta)
        {
            return _obtenerVenta.ObtenerVentaAsync(idVenta);
        }

        // ---------- Anulación (borrado lógico + reposición de stock) ----------

        public Task<ResultadoVenta> AnularVentaAsync(int idVenta, string motivo)
        {
            return _anularVenta.AnularVentaAsync(idVenta, motivo);
        }
    }
}
