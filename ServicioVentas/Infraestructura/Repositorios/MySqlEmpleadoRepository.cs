using MySql.Data.MySqlClient;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;

namespace ServicioVentas.Infraestructura.Repositorios
{
    /// <summary>
    /// Repositorio de empleados simplificado para el módulo de ventas.
    /// Solo se usa para obtener el nombre del empleado responsable en
    /// comprobantes de venta. No incluye credenciales ni tokens.
    /// </summary>
    public class MySqlEmpleadoRepository : IEmpleadoRepository
    {
        private const string Columnas = @"
            IdEmpleado,
            Nombre,
            Apellido,
            Rol,
            Telefono,
            CorreoElectronico,
            Estado,
            FechaRegistro,
            FechaActualizacion,
            IdEmpleadoResponsable";

        private readonly IDbConnectionFactory _connectionFactory;

        public MySqlEmpleadoRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public virtual async Task<List<Empleado>> ObtenerTodosAsync()
        {
            List<Empleado> empleados = new();

            string query = $@"
                SELECT {Columnas}
                FROM empleado
                ORDER BY Nombre ASC";

            using MySqlConnection connection = _connectionFactory.CreateConnection();
            using MySqlCommand command = new MySqlCommand(query, connection);

            await connection.OpenAsync();

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                empleados.Add(MapearEmpleado(reader));
            }

            return empleados;
        }

        public virtual async Task<int> InsertarAsync(Empleado entidad)
        {
            throw new NotImplementedException("No se usa insertar empleados desde ventas.");
        }

        private static Empleado MapearEmpleado(MySqlDataReader reader)
        {
            return new Empleado
            {
                IdEmpleado = reader.GetInt32("IdEmpleado"),
                Nombre = reader["Nombre"].ToString() ?? "",
                Apellido = reader["Apellido"].ToString() ?? "",
                Rol = reader["Rol"].ToString() ?? "",
                Telefono = reader["Telefono"].ToString() ?? "",
                CorreoElectronico = reader["CorreoElectronico"] == DBNull.Value
                    ? null
                    : reader["CorreoElectronico"].ToString(),
                Estado = (sbyte)reader.GetByte("Estado"),
                FechaRegistro = reader.GetDateTime("FechaRegistro"),
                FechaActualizacion = reader["FechaActualizacion"] == DBNull.Value
                    ? null
                    : reader.GetDateTime("FechaActualizacion"),
                IdEmpleadoResponsable = reader["IdEmpleadoResponsable"] == DBNull.Value
                    ? null
                    : reader.GetInt32("IdEmpleadoResponsable")
            };
        }
    }
}
