namespace ServicioVentas.Dominio.Puertos
{
    public interface IGestionarRepository<T> : IRepository<T>
    {
        Task ActualizarAsync(T entidad);

        Task CambiarEstadoAsync(int id, int idEmpleadoResponsable);

        Task<List<T>> MostrarActivosAsync();
    }
}
