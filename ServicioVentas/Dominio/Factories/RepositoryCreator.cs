namespace ServicioVentas.Dominio.Factories
{
    /// <summary>
    /// Base del patrón Factory Method para repositorios. Cada creador
    /// concreto produce un repositorio sobre la conexión/transacción del
    /// request, de modo que las fachadas y casos de uso dependan solo del
    /// contrato de dominio y no de las clases MySQL.
    ///
    /// Vive en Dominio para que la Capa de Aplicación dependa de una
    /// abstracción del dominio y no de Infraestructura (regla de dependencias
    /// de arquitectura limpia).
    /// </summary>
    /// <typeparam name="TContrato">Contrato de repositorio del dominio.</typeparam>
    public abstract class RepositoryCreator<TContrato>
    {
        /// <summary>Crea y devuelve una instancia del repositorio del contrato.</summary>
        public abstract TContrato CreateRepository();
    }
}
