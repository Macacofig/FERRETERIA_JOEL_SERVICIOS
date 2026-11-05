using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace ServicioReportes.Infraestructura.Conexion
{
    /// <summary>
    /// Crea conexiones a partir de 'ConnectionStrings:MySqlConnection'.
    /// La cadena se valida al arrancar para fallar con un mensaje claro si
    /// falta la variable de entorno o el appsettings.
    /// </summary>
    public sealed class MySqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _cadenaConexion;

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

            _cadenaConexion = cadena;
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_cadenaConexion);
        }
    }
}
