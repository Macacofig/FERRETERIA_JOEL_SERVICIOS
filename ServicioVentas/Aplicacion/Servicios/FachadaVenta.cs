using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Dominio.Constantes;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Dominio.Factories;

namespace ServicioVentas.Aplicacion.Servicios
{
    /// <summary>
    /// Fachada (Facade) del proceso de REGISTRO de venta. Presenta una
    /// única interfaz de alto nivel a la pantalla de venta, coordinando
    /// clientes, el subsistema de stock (<see cref="FachadaActualizacionStock"/>),
    /// la cabecera/detalle de la venta, la unidad de trabajo (transacción).
    ///
    /// La pantalla de venta NO conoce repositorios, transacciones ni
    /// políticas: solo consume esta fachada. A su vez, esta fachada
    /// delega el inventario en la fachada de stock, de modo que el
    /// descuento de existencias queda encapsulado en su propio subsistema.
    /// Todo el flujo es asíncrono (async/await) de extremo a extremo.
    /// </summary>
    public class FachadaVenta
    {
        private readonly RepositoryCreator<IVentaRepository> _creadorVentas;
        private readonly RepositoryCreator<IDetalleVentaRepository> _creadorDetalles;
        private readonly RepositoryCreator<IEmpleadoRepository> _creadorEmpleados;
        private readonly RepositoryCreator<ICategoriaRepository> _creadorCategorias;
        private readonly RepositoryCreator<IProductoRepository> _creadorProductos;
        private readonly IUnidadDeTrabajo _unidadDeTrabajo;
        private readonly IContextoUsuario _contextoUsuario;
        private readonly FachadaActualizacionStock _stock;
        private readonly ServicioCliente _servicioCliente;

        private IVentaRepository? _ventas;
        private IDetalleVentaRepository? _detalles;
        private IEmpleadoRepository? _empleados;
        private ICategoriaRepository? _categorias;
        private IProductoRepository? _productos;

        public FachadaVenta(
            RepositoryCreator<IVentaRepository> creadorVentas,
            RepositoryCreator<IDetalleVentaRepository> creadorDetalles,
            RepositoryCreator<IEmpleadoRepository> creadorEmpleados,
            RepositoryCreator<ICategoriaRepository> creadorCategorias,
            RepositoryCreator<IProductoRepository> creadorProductos,
            IUnidadDeTrabajo unidadDeTrabajo,
            IContextoUsuario contextoUsuario,
            FachadaActualizacionStock stock,
            ServicioCliente servicioCliente)
        {
            _creadorVentas = creadorVentas;
            _creadorDetalles = creadorDetalles;
            _creadorEmpleados = creadorEmpleados;
            _creadorCategorias = creadorCategorias;
            _creadorProductos = creadorProductos;
            _unidadDeTrabajo = unidadDeTrabajo;
            _contextoUsuario = contextoUsuario;
            _stock = stock;
            _servicioCliente = servicioCliente;
        }

        private IVentaRepository Ventas =>
            _ventas ??= _creadorVentas.CreateRepository();

        private IDetalleVentaRepository Detalles =>
            _detalles ??= _creadorDetalles.CreateRepository();

        private IEmpleadoRepository Empleados =>
            _empleados ??= _creadorEmpleados.CreateRepository();

        private ICategoriaRepository Categorias =>
            _categorias ??= _creadorCategorias.CreateRepository();

        private IProductoRepository Productos =>
            _productos ??= _creadorProductos.CreateRepository();

        // ---------- Búsquedas previas a la inserción ----------

        public async Task<List<Cliente>> BuscarClientesAsync(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
            {
                return new List<Cliente>();
            }

            return await _servicioCliente.BuscarAsync(termino.Trim());
        }

        public async Task<List<Producto>> BuscarProductosAsync(string termino)
        {
            List<Producto> activos = await _stock.ListarActivosAsync();

            if (string.IsNullOrWhiteSpace(termino))
            {
                return activos.Take(20).ToList();
            }

            string patron = termino.Trim();

            return activos
                .Where(p =>
                    p.Nombre.Contains(patron, StringComparison.OrdinalIgnoreCase) ||
                    p.Codigo.Contains(patron, StringComparison.OrdinalIgnoreCase))
                .Take(20)
                .ToList();
        }

        /// <summary>
        /// Catálogo completo para la pantalla de consulta de productos. A
        /// diferencia de la búsqueda de venta, incluye también los inactivos.
        /// </summary>
        public Task<List<Producto>> ListarProductosAsync()
        {
            return Productos.ObtenerTodosAsync();
        }

        public Task<Producto?> ObtenerProductoAsync(int idProducto)
        {
            return _stock.ObtenerParaVentaAsync(idProducto);
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
            return _servicioCliente.CrearAsync(
                ci,
                nombre,
                apellido);
        }

        public async Task<(LineaVentaDetalle? Linea, string? Error)> CalcularLineaAsync(
            int idProducto,
            int cantidad)
        {
            if (cantidad <= 0)
            {
                return (null, "La cantidad debe ser mayor que cero.");
            }

            Producto? producto = await ObtenerProductoAsync(idProducto);

            if (producto is null || producto.Estado != 1)
            {
                return (null, "El producto seleccionado no existe o está inactivo.");
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

        public ResumenVenta CalcularResumen(IEnumerable<LineaVentaDetalle> lineas)
        {
            decimal total = lineas.Sum(l => l.TotalProducto);

            return new ResumenVenta
            {
                MontoTotal = Math.Round(total, 2)
            };
        }

        // ---------- Registro de la venta (transacción) ----------

        public async Task<ResultadoVenta> RegistrarVentaAsync(SolicitudVenta solicitud)
        {
            if (solicitud is null)
            {
                return ResultadoVenta.Fallido(
                    "La solicitud de venta es obligatoria.");
            }

            if (solicitud.IdCliente <= 0)
            {
                return ResultadoVenta.Fallido("Debe seleccionar un cliente.");
            }

            if (solicitud.Lineas is null || solicitud.Lineas.Count == 0)
            {
                return ResultadoVenta.Fallido(
                    "Debe agregar al menos un producto a la venta.");
            }

            if (string.IsNullOrWhiteSpace(solicitud.MetodoPago))
            {
                return ResultadoVenta.Fallido(
                    "Debe indicar el método de pago.");
            }

            int idEmpleado = _contextoUsuario.IdEmpleado;

            if (idEmpleado <= 0)
            {
                return ResultadoVenta.Fallido(
                    "Debe iniciar sesión para registrar la venta.");
            }

            Dictionary<int, int> cantidades = new();

            foreach (LineaVentaSolicitada linea in solicitud.Lineas)
            {
                if (linea.Cantidad <= 0)
                {
                    return ResultadoVenta.Fallido(
                        "Todas las cantidades deben ser mayores que cero.");
                }

                cantidades[linea.IdProducto] = cantidades.TryGetValue(
                    linea.IdProducto,
                    out int acumulada)
                        ? acumulada + linea.Cantidad
                        : linea.Cantidad;
            }

            string metodoPago = solicitud.MetodoPago.Trim();
            string estadoPago = string.IsNullOrWhiteSpace(solicitud.EstadoPago)
                ? EstadoPago.Pagado
                : solicitud.EstadoPago.Trim();

            // Por ahora solo se registran ventas pagadas.
            if (!string.Equals(
                    estadoPago,
                    EstadoPago.Pagado,
                    StringComparison.OrdinalIgnoreCase))
            {
                return ResultadoVenta.Fallido(
                    "Por el momento solo se registran ventas con el pago completado.");
            }

            await _unidadDeTrabajo.IniciarAsync();

            try
            {
                Cliente? cliente =
                    await _servicioCliente.ObtenerPorIdAsync(solicitud.IdCliente);

                if (cliente is null || cliente.Estado != 1)
                {
                    await _unidadDeTrabajo.RevertirAsync();
                    return ResultadoVenta.Fallido(
                        "El cliente seleccionado no existe o está inactivo.");
                }

                List<DetalleVenta> detalles = new();
                List<LineaVentaDetalle> lineasCalculadas = new();
                decimal total = 0m;

                foreach (KeyValuePair<int, int> par in cantidades)
                {
                    Producto? producto =
                        await _stock.ObtenerParaVentaAsync(par.Key);

                    if (producto is null)
                    {
                        await _unidadDeTrabajo.RevertirAsync();
                        return ResultadoVenta.Fallido(
                            $"El producto con id {par.Key} no está disponible.");
                    }

                    if (par.Value > producto.CantidadStock)
                    {
                        await _unidadDeTrabajo.RevertirAsync();
                        return ResultadoVenta.Fallido(
                            $"Stock insuficiente para '{producto.Nombre}'. " +
                            $"Disponible: {producto.CantidadStock}.");
                    }

                    if (!await _stock.DescontarAsync(par.Key, par.Value))
                    {
                        await _unidadDeTrabajo.RevertirAsync();
                        return ResultadoVenta.Fallido(
                            $"No se pudo descontar el stock de '{producto.Nombre}'.");
                    }

                    decimal totalProducto =
                        Math.Round(producto.PrecioVenta * par.Value, 2);

                    total += totalProducto;

                    detalles.Add(new DetalleVenta
                    {
                        IdProducto = producto.IdProducto,
                        Cantidad = par.Value,
                        PrecioUnitario = producto.PrecioVenta,
                        TotalProducto = totalProducto,
                        IdEmpleadoResponsable = idEmpleado
                    });

                    lineasCalculadas.Add(new LineaVentaDetalle
                    {
                        IdProducto = producto.IdProducto,
                        Codigo = producto.Codigo,
                        Nombre = producto.Nombre,
                        UnidadMedida = producto.UnidadMedida,
                        Cantidad = par.Value,
                        PrecioUnitario = producto.PrecioVenta,
                        TotalProducto = totalProducto,
                        CantidadStock = producto.CantidadStock - par.Value
                    });
                }

                Venta venta = new()
                {
                    EstadoVenta = EstadoVenta.Registrada,
                    IdCliente = cliente.IdCliente,
                    MetodoPago = metodoPago,
                    EstadoPago = estadoPago,
                    MontoTotal = Math.Round(total, 2),
                    IdEmpleadoResponsable = idEmpleado
                };

                int idVenta = await Ventas.InsertarAsync(venta);
                await Ventas.AsignarCodigoAsync(idVenta);

                foreach (DetalleVenta detalle in detalles)
                {
                    detalle.IdVenta = idVenta;
                    await Detalles.InsertarAsync(detalle);
                }

                await _unidadDeTrabajo.ConfirmarAsync();

                try
                {
                    await Categorias.IncrementarPorcentajeGananciaAsync();
                }
                catch
                {
                    // El recálculo auxiliar no debe anular una venta ya confirmada
                }

                Venta guardada =
                    await Ventas.ObtenerPorIdAsync(idVenta) ?? venta;

                return ResultadoVenta.Correcto(guardada, lineasCalculadas);
            }
            catch (Exception)
            {
                await _unidadDeTrabajo.RevertirAsync();

                return ResultadoVenta.Fallido(
                    "No se pudo registrar la venta. Intente nuevamente.");
            }
        }

        // ---------- Comprobante / recibo ----------

        public async Task<ComprobanteVenta?> ObtenerComprobanteAsync(int idVenta)
        {
            Venta? venta = await Ventas.ObtenerPorIdAsync(idVenta);

            if (venta is null)
            {
                return null;
            }

            Cliente? cliente =
                await _servicioCliente.ObtenerPorIdAsync(venta.IdCliente);

            string nombreCliente = cliente is null
                ? "Cliente eliminado"
                : $"{cliente.Nombre} {cliente.Apellido}".Trim();

            List<Empleado> todos = await Empleados.ObtenerTodosAsync();

            return new ComprobanteVenta
            {
                Venta = venta,
                NombreCliente = nombreCliente,
                CiCliente = cliente?.Ci ?? string.Empty,
                NombreEmpleadoResponsable = NombreDe(todos, venta.IdEmpleadoResponsable),
                NombreEmpleadoAnulacion = venta.IdEmpleadoAnulacion is not null
                    ? NombreDe(todos, venta.IdEmpleadoAnulacion.Value)
                    : null,
                Lineas = await _stock.MapearLineasAsync(
                    await Detalles.ObtenerPorVentaAsync(idVenta))
            };
        }

        private static string NombreDe(List<Empleado> empleados, int idEmpleado)
        {
            Empleado? empleado = empleados
                .FirstOrDefault(e => e.IdEmpleado == idEmpleado);

            return empleado is null
                ? $"Empleado #{idEmpleado}"
                : $"{empleado.Nombre} {empleado.Apellido}".Trim();
        }
    }
}
