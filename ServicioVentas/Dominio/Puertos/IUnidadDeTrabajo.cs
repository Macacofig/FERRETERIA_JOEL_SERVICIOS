namespace ServicioVentas.Dominio.Puertos
{
    /// <summary>
    /// Puerto para coordinar una transacción de base de datos de forma
    /// asíncrona. La aplicación abre, confirma o revierte; la
    /// infraestructura provee la conexión y transacción reales.
    /// </summary>
    public interface IUnidadDeTrabajo
    {
        Task IniciarAsync();

        Task ConfirmarAsync();

        Task RevertirAsync();
    }
}
