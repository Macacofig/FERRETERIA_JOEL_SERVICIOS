namespace ServicioVentas.Dominio.Factories
{
    /// <summary>
    /// Base del patrón Factory Method para repositorios. Cada creador
    /// concreto produce un repositorio sobre la conexión/transacción del
    /// request, de modo que las fachadas dependan solo del contrato de
    /// dominio y no de las clases MySQL.
    /// </summary>
    public abstract class RepositoryCreator<TContrato>
    {
        public abstract TContrato CreateRepository();
    }
}
