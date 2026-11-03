using MySql.Data.MySqlClient;

namespace ServicioVentas.Infraestructura.Conexion
{
    public sealed class MySqlConexionTransaccional : IConexionTransaccional
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private MySqlConnection? _conexion;
        private MySqlTransaction? _transaccion;
        private bool _disposed;

        public MySqlConexionTransaccional(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public MySqlTransaction? Transaccion => _transaccion;

        public async Task<MySqlConnection> ObtenerConexionAsync()
        {
            if (_conexion is null)
            {
                _conexion = _connectionFactory.CreateConnection();
                await _conexion.OpenAsync();
            }

            return _conexion;
        }

        public async Task IniciarTransaccionAsync()
        {
            if (_transaccion is not null)
            {
                throw new InvalidOperationException(
                    "Ya existe una transacción activa en esta conexión.");
            }

            MySqlConnection conexion = await ObtenerConexionAsync();
            _transaccion = await conexion.BeginTransactionAsync();
        }

        public async Task ConfirmarAsync()
        {
            if (_transaccion is null)
            {
                return;
            }

            await _transaccion.CommitAsync();
            await _transaccion.DisposeAsync();
            _transaccion = null;
        }

        public async Task RevertirAsync()
        {
            if (_transaccion is null)
            {
                return;
            }

            await _transaccion.RollbackAsync();
            await _transaccion.DisposeAsync();
            _transaccion = null;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _transaccion?.Dispose();
            _conexion?.Dispose();
            _transaccion = null;
            _conexion = null;
            _disposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            if (_transaccion is not null)
            {
                await _transaccion.DisposeAsync();
                _transaccion = null;
            }

            if (_conexion is not null)
            {
                await _conexion.DisposeAsync();
                _conexion = null;
            }

            _disposed = true;
        }
    }
}
