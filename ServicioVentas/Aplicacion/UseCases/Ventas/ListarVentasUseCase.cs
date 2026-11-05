using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.UseCases.Clientes;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Ventas
{
    /// <summary>
    /// Caso de uso: listar el historial de ventas con el nombre del cliente
    /// resuelto para la presentación.
    /// </summary>
    public interface IListarVentasUseCase
    {
        Task<List<VentaListado>> ListarVentasListadoAsync();
    }

    public class ListarVentasUseCase : IListarVentasUseCase
    {
        private readonly RepositoryCreator<IVentaRepository> _creadorVentas;
        private readonly IObtenerClienteUseCase _obtenerCliente;
        private IVentaRepository? _ventas;

        public ListarVentasUseCase(
            RepositoryCreator<IVentaRepository> creadorVentas,
            IObtenerClienteUseCase obtenerCliente)
        {
            _creadorVentas = creadorVentas;
            _obtenerCliente = obtenerCliente;
        }

        private IVentaRepository Ventas =>
            _ventas ??= _creadorVentas.CreateRepository();

        public async Task<List<VentaListado>> ListarVentasListadoAsync()
        {
            List<Venta> ventas = await Ventas.ObtenerTodosAsync();

            Dictionary<int, string> nombresCliente = new();

            foreach (int idCliente in ventas.Select(v => v.IdCliente).Distinct())
            {
                Cliente? cliente =
                    await _obtenerCliente.ObtenerPorIdAsync(idCliente);

                if (cliente is not null)
                {
                    nombresCliente[idCliente] =
                        $"{cliente.Nombre} {cliente.Apellido}".Trim();
                }
            }

            return ventas
                .Select(v => new VentaListado
                {
                    Venta = v,
                    NombreCliente = nombresCliente.TryGetValue(
                        v.IdCliente,
                        out string? nombre)
                        ? nombre
                        : "Cliente eliminado"
                })
                .ToList();
        }
    }
}
