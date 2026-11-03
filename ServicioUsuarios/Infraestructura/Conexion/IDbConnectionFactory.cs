using MySql.Data.MySqlClient;

namespace FERRETERIA__Joel.Infraestructura.Conexion
{
    public interface IDbConnectionFactory
    {
        MySqlConnection CreateConnection();
    }
}