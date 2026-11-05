using ServicioVentas.Aplicacion.UseCases.Stock;
using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Aplicacion.UseCases.Ventas
{
    /// <summary>
    /// Caso de uso: buscar productos activos por nombre o código para
    /// agregarlos a la venta.
    /// </summary>
    public interface IBuscarProductosUseCase
    {
        Task<List<Producto>> BuscarProductosAsync(string termino);
    }

    public class BuscarProductosUseCase : IBuscarProductosUseCase
    {
        private readonly IListarProductosActivosUseCase _listarActivos;

        public BuscarProductosUseCase(
            IListarProductosActivosUseCase listarActivos)
        {
            _listarActivos = listarActivos;
        }

        public async Task<List<Producto>> BuscarProductosAsync(string termino)
        {
            List<Producto> activos = await _listarActivos.ListarActivosAsync();

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
    }
}
