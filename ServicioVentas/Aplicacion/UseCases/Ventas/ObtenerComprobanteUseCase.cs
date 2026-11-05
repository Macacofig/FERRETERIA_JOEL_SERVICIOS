using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.UseCases.Clientes;
using ServicioVentas.Aplicacion.UseCases.Stock;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Ventas
{
    /// <summary>
    /// Caso de uso: armar el comprobante de una venta con el nombre del
    /// cliente, los responsables y las líneas enriquecidas con el catálogo.
    /// </summary>
    public interface IObtenerComprobanteUseCase
    {
        Task<ComprobanteVenta?> ObtenerComprobanteAsync(int idVenta);
    }

    public class ObtenerComprobanteUseCase : IObtenerComprobanteUseCase
    {
        private readonly RepositoryCreator<IVentaRepository> _creadorVentas;
        private readonly RepositoryCreator<IDetalleVentaRepository> _creadorDetalles;
        private readonly RepositoryCreator<IEmpleadoRepository> _creadorEmpleados;
        private readonly IObtenerClienteUseCase _obtenerCliente;
        private readonly IMapearLineasVentaUseCase _mapearLineas;

        private IVentaRepository? _ventas;
        private IDetalleVentaRepository? _detalles;
        private IEmpleadoRepository? _empleados;

        public ObtenerComprobanteUseCase(
            RepositoryCreator<IVentaRepository> creadorVentas,
            RepositoryCreator<IDetalleVentaRepository> creadorDetalles,
            RepositoryCreator<IEmpleadoRepository> creadorEmpleados,
            IObtenerClienteUseCase obtenerCliente,
            IMapearLineasVentaUseCase mapearLineas)
        {
            _creadorVentas = creadorVentas;
            _creadorDetalles = creadorDetalles;
            _creadorEmpleados = creadorEmpleados;
            _obtenerCliente = obtenerCliente;
            _mapearLineas = mapearLineas;
        }

        private IVentaRepository Ventas =>
            _ventas ??= _creadorVentas.CreateRepository();

        private IDetalleVentaRepository Detalles =>
            _detalles ??= _creadorDetalles.CreateRepository();

        private IEmpleadoRepository Empleados =>
            _empleados ??= _creadorEmpleados.CreateRepository();

        public async Task<ComprobanteVenta?> ObtenerComprobanteAsync(int idVenta)
        {
            Venta? venta = await Ventas.ObtenerPorIdAsync(idVenta);

            if (venta is null)
            {
                return null;
            }

            Cliente? cliente =
                await _obtenerCliente.ObtenerPorIdAsync(venta.IdCliente);

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
                Lineas = await _mapearLineas.MapearLineasAsync(
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
