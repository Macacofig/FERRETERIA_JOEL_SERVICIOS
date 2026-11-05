using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.UseCases.Clientes;
using ServicioVentas.Aplicacion.UseCases.Stock;
using ServicioVentas.Dominio.Constantes;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Ventas
{
    /// <summary>
    /// Caso de uso: registrar una venta completa. Valida la solicitud,
    /// descuenta stock, guarda cabecera y detalle dentro de una transacción y
    /// recalcula los porcentajes de ganancia al finalizar.
    /// </summary>
    public interface IRegistrarVentaUseCase
    {
        Task<ResultadoVenta> RegistrarVentaAsync(SolicitudVenta solicitud);
    }

    public class RegistrarVentaUseCase : IRegistrarVentaUseCase
    {
        private readonly RepositoryCreator<IVentaRepository> _creadorVentas;
        private readonly RepositoryCreator<IDetalleVentaRepository> _creadorDetalles;
        private readonly RepositoryCreator<ICategoriaRepository> _creadorCategorias;
        private readonly IUnidadDeTrabajo _unidadDeTrabajo;
        private readonly IContextoUsuario _contextoUsuario;
        private readonly IObtenerClienteUseCase _obtenerCliente;
        private readonly IObtenerProductoParaVentaUseCase _obtenerProducto;
        private readonly IDescontarStockUseCase _descontarStock;

        private IVentaRepository? _ventas;
        private IDetalleVentaRepository? _detalles;
        private ICategoriaRepository? _categorias;

        public RegistrarVentaUseCase(
            RepositoryCreator<IVentaRepository> creadorVentas,
            RepositoryCreator<IDetalleVentaRepository> creadorDetalles,
            RepositoryCreator<ICategoriaRepository> creadorCategorias,
            IUnidadDeTrabajo unidadDeTrabajo,
            IContextoUsuario contextoUsuario,
            IObtenerClienteUseCase obtenerCliente,
            IObtenerProductoParaVentaUseCase obtenerProducto,
            IDescontarStockUseCase descontarStock)
        {
            _creadorVentas = creadorVentas;
            _creadorDetalles = creadorDetalles;
            _creadorCategorias = creadorCategorias;
            _unidadDeTrabajo = unidadDeTrabajo;
            _contextoUsuario = contextoUsuario;
            _obtenerCliente = obtenerCliente;
            _obtenerProducto = obtenerProducto;
            _descontarStock = descontarStock;
        }

        private IVentaRepository Ventas =>
            _ventas ??= _creadorVentas.CreateRepository();

        private IDetalleVentaRepository Detalles =>
            _detalles ??= _creadorDetalles.CreateRepository();

        private ICategoriaRepository Categorias =>
            _categorias ??= _creadorCategorias.CreateRepository();

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
                    await _obtenerCliente.ObtenerPorIdAsync(solicitud.IdCliente);

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
                        await _obtenerProducto.ObtenerParaVentaAsync(par.Key);

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

                    if (!await _descontarStock.DescontarAsync(par.Key, par.Value))
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
    }
}
