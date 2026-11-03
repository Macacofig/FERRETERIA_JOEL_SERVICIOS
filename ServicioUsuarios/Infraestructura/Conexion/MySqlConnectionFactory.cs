using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace FERRETERIA__Joel.Infraestructura.Conexion
{
    public class MySqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public MySqlConnectionFactory(IConfiguration configuration)
        {
            string? cadena = configuration.GetConnectionString("MySqlConnection");

            if (string.IsNullOrWhiteSpace(cadena))
            {
                string servidor = configuration["MYSQL_HOST"] ?? "localhost";
                string puerto = configuration["MYSQL_PUERTO_HOST"] ?? "3307";
                string baseDatos = configuration["MYSQL_DATABASE"] ?? "bdferreteria";
                string usuario = configuration["MYSQL_USER"] ?? "root";
                string clave = configuration["MYSQL_ROOT_PASSWORD"] ?? string.Empty;

                cadena = $"Server={servidor};Port={puerto};Database={baseDatos};Uid={usuario};Pwd={clave};";
            }

            if (string.IsNullOrWhiteSpace(cadena))
            {
                throw new InvalidOperationException(
                    "Falta la configuraciÃ³n de MySQL. Defina MYSQL_DATABASE y "
                    + "MYSQL_ROOT_PASSWORD en el archivo .env.");
            }

            _connectionString = cadena;
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}
