namespace FERRETERIA__Joel.Infraestructura.Factories
{
    /// <summary>
    /// Base del patrón Factory Method para repositorios. Cada creador
    /// concreto produce un repositorio sobre la conexión configurada, de modo
    /// que los casos de uso dependan solo del contrato de dominio y no de las
    /// clases MySQL.
    /// </summary>
    public abstract class RepositoryCreator<TContrato>
    {
        public abstract TContrato CreateRepository();
    }
}
