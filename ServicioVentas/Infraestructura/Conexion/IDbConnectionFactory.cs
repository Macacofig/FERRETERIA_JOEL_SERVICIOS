using MySql.Data.MySqlClient;

namespace ServicioVentas.Infraestructura.Conexion
{
    public interface IDbConnectionFactory
    {
        MySqlConnection CreateConnection();
    }
}
