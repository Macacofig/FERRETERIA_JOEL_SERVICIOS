using MySql.Data.MySqlClient;

namespace ServicioVentas.Infraestructura.Conexion
{
    /// <summary>
    /// Contexto de conexión compartido por request. Permite que varios
    /// repositorios ejecuten sus comandos de forma asíncrona sobre la
    /// MISMA conexión y la MISMA transacción MySQL. La unidad de trabajo
    /// abre, confirma o revierte; los repositorios solo la consumen.
    ///
    /// Todas las operaciones de E/S son asíncronas (OpenAsync,
    /// BeginTransactionAsync, CommitAsync, RollbackAsync) para no
    /// bloquear el hilo durante la venta.
    /// </summary>
    public interface IConexionTransaccional : IAsyncDisposable, IDisposable
    {
        MySqlTransaction? Transaccion { get; }

        Task<MySqlConnection> ObtenerConexionAsync();

        Task IniciarTransaccionAsync();

        Task ConfirmarAsync();

        Task RevertirAsync();
    }
}
