namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Contrato base de persistencia. Todas las operaciones son asÃ­ncronas
    /// para no bloquear el hilo durante el acceso a la base de datos.
    /// </summary>
    public interface IRepository<T>
    {
        Task<List<T>> ObtenerTodosAsync();

        Task<int> InsertarAsync(T entidad);
    }
}

