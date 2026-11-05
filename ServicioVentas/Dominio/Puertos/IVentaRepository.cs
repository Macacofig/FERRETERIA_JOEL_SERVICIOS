using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Dominio.Puertos
{
    /// <summary>
    /// Contrato propio del repositorio de ventas. Extiende el CRUD genérico
    /// con las operaciones del flujo de caja: folio, consulta por id y
    /// anulación lógica.
    /// </summary>
    public interface IVentaRepository : IRepository<Venta>
    {
        Task AsignarCodigoAsync(int idVenta);

        Task<Venta?> ObtenerPorIdAsync(int idVenta);

        Task<bool> MarcarAnuladaAsync(
            int idVenta,
            int idEmpleadoAnulacion,
            string motivo,
            DateTime fechaAnulacion,
            DateTime fechaActualizacion);
    }
}
