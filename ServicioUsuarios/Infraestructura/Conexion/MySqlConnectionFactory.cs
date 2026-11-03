using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace FERRETERIA__Joel.Infraestructura.Conexion
{
    public class MySqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public MySqlConnectionFactory(IConfiguration configuration)
        {
            string? cadena =
                configuration.GetConnectionString("MySqlConnection");

            if (string.IsNullOrWhiteSpace(cadena))
            {
                throw new InvalidOperationException(
                    "Falta la cadena de conexión 'ConnectionStrings:MySqlConnection'. "
                    + "Defina la variable de entorno ConnectionStrings__MySqlConnection "
                    + "(archivo .env de la raíz o docker-compose.yml).");
            }

            _connectionString = cadena;
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}