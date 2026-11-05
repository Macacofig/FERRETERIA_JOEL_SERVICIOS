using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.UseCases.Clientes;
using ServicioVentas.Aplicacion.UseCases.Ventas;
using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Aplicacion.Servicios
{
    /// <summary>
    /// Fachada (Facade) del proceso de REGISTRO de venta. Presenta una única
    /// interfaz de alto nivel a la pantalla de venta y delega cada operación
    /// en su caso de uso correspondiente.
    ///
    /// La pantalla de venta NO conoce repositorios, transacciones ni
    /// políticas: solo consume esta fachada. El inventario queda encapsulado
    /// en el subsistema de stock. Todo el flujo es asíncrono de extremo a
    /// extremo.
    /// </summary>
    public class FachadaVenta :
        IBuscarClientesUseCase,
        ICrearClienteUseCase,
        IBuscarProductosUseCase,
        IListarProductosUseCase,
        IObtenerProductoUseCase,
        ICalcularLineaVentaUseCase,
        ICalcularResumenVentaUseCase,
        IRegistrarVentaUseCase,
        IObtenerComprobanteUseCase
    {
        private readonly IBuscarClientesUseCase _buscarClientes;
        private readonly ICrearClienteUseCase _crearCliente;
        private readonly IBuscarProductosUseCase _buscarProductos;
        private readonly IListarProductosUseCase _listarProductos;
        private readonly IObtenerProductoUseCase _obtenerProducto;
        private readonly ICalcularLineaVentaUseCase _calcularLinea;
        private readonly ICalcularResumenVentaUseCase _calcularResumen;
        private readonly IRegistrarVentaUseCase _registrarVenta;
        private readonly IObtenerComprobanteUseCase _obtenerComprobante;

        public FachadaVenta(
            IBuscarClientesUseCase buscarClientes,
            ICrearClienteUseCase crearCliente,
            IBuscarProductosUseCase buscarProductos,
            IListarProductosUseCase listarProductos,
            IObtenerProductoUseCase obtenerProducto,
            ICalcularLineaVentaUseCase calcularLinea,
            ICalcularResumenVentaUseCase calcularResumen,
            IRegistrarVentaUseCase registrarVenta,
            IObtenerComprobanteUseCase obtenerComprobante)
        {
            _buscarClientes = buscarClientes;
            _crearCliente = crearCliente;
            _buscarProductos = buscarProductos;
            _listarProductos = listarProductos;
            _obtenerProducto = obtenerProducto;
            _calcularLinea = calcularLinea;
            _calcularResumen = calcularResumen;
            _registrarVenta = registrarVenta;
            _obtenerComprobante = obtenerComprobante;
        }

        // ---------- Búsquedas previas a la inserción ----------

        public Task<List<Cliente>> BuscarClientesAsync(string termino)
        {
            return _buscarClientes.BuscarClientesAsync(termino);
        }

        public Task<List<Producto>> BuscarProductosAsync(string termino)
        {
            return _buscarProductos.BuscarProductosAsync(termino);
        }

        /// <summary>
        /// Catálogo completo para la pantalla de consulta de productos. A
        /// diferencia de la búsqueda de venta, incluye también los inactivos.
        /// </summary>
        public Task<List<Producto>> ListarProductosAsync()
        {
            return _listarProductos.ListarProductosAsync();
        }

        public Task<Producto?> ObtenerProductoAsync(int idProducto)
        {
            return _obtenerProducto.ObtenerProductoAsync(idProducto);
        }

        /// <summary>
        /// Alta rápida de un cliente desde la propia pantalla de venta. Se
        /// delega en el caso de uso de clientes para no duplicar reglas.
        /// </summary>
        public Task<ResultadoCliente> CrearClienteAsync(
            string ci,
            string nombre,
            string apellido)
        {
            return _crearCliente.CrearClienteAsync(ci, nombre, apellido);
        }

        public Task<(LineaVentaDetalle? Linea, string? Error)> CalcularLineaAsync(
            int idProducto,
            int cantidad)
        {
            return _calcularLinea.CalcularLineaAsync(idProducto, cantidad);
        }

        public ResumenVenta CalcularResumen(IEnumerable<LineaVentaDetalle> lineas)
        {
            return _calcularResumen.CalcularResumen(lineas);
        }

        // ---------- Registro de la venta (transacción) ----------

        public Task<ResultadoVenta> RegistrarVentaAsync(SolicitudVenta solicitud)
        {
            return _registrarVenta.RegistrarVentaAsync(solicitud);
        }

        // ---------- Comprobante / recibo ----------

        public Task<ComprobanteVenta?> ObtenerComprobanteAsync(int idVenta)
        {
            return _obtenerComprobante.ObtenerComprobanteAsync(idVenta);
        }
    }
}
