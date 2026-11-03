using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using ServicioUsuarios.Application.Ports;

namespace ServicioUsuarios.Adapters.Out.MySql;

public sealed class MySqlConnectionFactory : IMySqlConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IOptions<MySqlOptions> options)
    {
        _connectionString = options.Value.MySql;
    }

    public MySqlConnection Create()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("La cadena de conexión MySql no está configurada.");

        return new MySqlConnection(_connectionString);
    }
}
