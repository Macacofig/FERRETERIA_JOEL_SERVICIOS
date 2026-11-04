using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Dominio.Puertos
{
    /// <summary>
    /// Contrato del repositorio de clientes usado por el flujo de venta:
    /// búsqueda por CI/nombre, control de CI duplicado y consulta por id.
    /// </summary>
    public interface IClienteRepository : IRepository<Cliente>
    {
        Task<List<Cliente>> BuscarAsync(string termino);

        Task<bool> ExisteCiAsync(string ci);

        Task<Cliente?> ObtenerPorIdAsync(int idCliente);
    }
}
