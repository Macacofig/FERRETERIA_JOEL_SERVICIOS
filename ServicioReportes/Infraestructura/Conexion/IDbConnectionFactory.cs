using MySql.Data.MySqlClient;

namespace ServicioReportes.Infraestructura.Conexion
{
    /// <summary>Fábrica de conexiones ADO.NET hacia MySQL.</summary>
    public interface IDbConnectionFactory
    {
        MySqlConnection CreateConnection();
    }
}
