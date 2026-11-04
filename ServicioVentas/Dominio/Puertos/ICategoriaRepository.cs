using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Dominio.Puertos
{
    /// <summary>
    /// Contrato del repositorio de categorías. El módulo de ventas solo usa
    /// la consulta por id y el recálculo auxiliar de porcentajes de ganancia
    /// que se ejecuta después de registrar o anular una venta.
    /// </summary>
    public interface ICategoriaRepository : IGestionarRepository<Categoria>
    {
        Task<Categoria?> ObtenerPorIdAsync(int id);

        Task<int> CountAsync();

        Task IncrementarPorcentajeGananciaAsync(
            int idCategoria = 0,
            decimal valor = 0m);
    }
}
