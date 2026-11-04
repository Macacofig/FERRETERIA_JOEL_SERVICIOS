using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Dominio.Puertos
{
    /// <summary>
    /// Contrato del repositorio de líneas de venta. Además del CRUD
    /// genérico expone la lectura de las líneas de una venta concreta.
    /// </summary>
    public interface IDetalleVentaRepository : IRepository<DetalleVenta>
    {
        Task<List<DetalleVenta>> ObtenerPorVentaAsync(int idVenta);
    }
}
