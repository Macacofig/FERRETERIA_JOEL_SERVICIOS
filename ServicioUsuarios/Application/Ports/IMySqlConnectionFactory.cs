using MySql.Data.MySqlClient;

namespace ServicioUsuarios.Application.Ports;

public interface IMySqlConnectionFactory
{
    MySqlConnection Create();
}
