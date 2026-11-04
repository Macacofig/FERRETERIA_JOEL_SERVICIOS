using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace ServicioVentas.Infraestructura.Repositorios
{
    /// <summary>
    /// Repositorio de clientes. Implementa el contrato genérico
    /// <see cref="IRepository{T}"/> y expone operaciones propias del flujo de
    /// venta (búsqueda y consulta por id) como métodos concretos que se
    /// inyectan directamente por DI. Ningún método dispone la conexión: su
    /// ciclo de vida lo administra <see cref="IConexionTransaccional"/>.
    /// </summary>
    public class MySqlClienteRepository : IClienteRepository
    {
        private const string Columnas = @"
            IdCliente,
            Ci,
            Nombre,
            Apellido,
            Estado,
            FechaRegistro,
            FechaActualizacion,
            IdEmpleadoResponsable";

        private readonly IConexionTransaccional _conexion;

        public MySqlClienteRepository(IConexionTransaccional conexion)
        {
            _conexion = conexion;
        }

        private async Task<MySqlCommand> CrearComandoAsync(string query)
        {
            MySqlCommand command = new(query, await _conexion.ObtenerConexionAsync());

            if (_conexion.Transaccion is not null)
            {
                command.Transaction = _conexion.Transaccion;
            }

            return command;
        }

        // ================== Contrato genérico ==================

        public virtual async Task<List<Cliente>> ObtenerTodosAsync()
        {
            List<Cliente> clientes = new();

            string query = $@"
                SELECT {Columnas}
                FROM cliente
                ORDER BY Nombre ASC";

            using MySqlCommand command = await CrearComandoAsync(query);

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                clientes.Add(MapearCliente(reader));
            }

            return clientes;
        }

        public virtual async Task<int> InsertarAsync(Cliente cliente)
        {
            const string query = @"
                INSERT INTO cliente
                (
                    Ci,
                    Nombre,
                    Apellido,
                    Estado,
                    IdEmpleadoResponsable
                )
                VALUES
                (
                    @ci,
                    @nombre,
                    @apellido,
                    1,
                    @idEmpleadoResponsable
                )";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@ci", cliente.Ci);
            command.Parameters.AddWithValue("@nombre", cliente.Nombre);
            command.Parameters.AddWithValue("@apellido", cliente.Apellido);
            command.Parameters.AddWithValue(
                "@idEmpleadoResponsable",
                (object?)cliente.IdEmpleadoResponsable ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();

            return Convert.ToInt32(command.LastInsertedId);
        }

        // ================== Operaciones propias de Ventas ==================

        public virtual async Task<List<Cliente>> BuscarAsync(string termino)
        {
            List<Cliente> clientes = new();

            string query = $@"
                SELECT {Columnas}
                FROM cliente
                WHERE Estado = 1
                AND (
                        Ci LIKE @prefijo
                    OR Nombre LIKE @contiene
                    OR Apellido LIKE @contiene
                    OR CONCAT(Nombre, ' ', Apellido) LIKE @contiene
                )
                ORDER BY Nombre ASC
                LIMIT 20";

            // Evita que % o _ escritos por el usuario actúen como comodines.
            string escapado = termino
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@prefijo", $"{escapado}%");
            command.Parameters.AddWithValue("@contiene", $"%{escapado}%");

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                clientes.Add(MapearCliente(reader));
            }

            return clientes;
        }

        public virtual async Task<bool> ExisteCiAsync(string ci)
        {
            const string query = @"
                SELECT 1
                FROM cliente
                WHERE Ci = @ci
                LIMIT 1";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@ci", ci);

            return await command.ExecuteScalarAsync() is not null;
        }

        public virtual async Task<Cliente?> ObtenerPorIdAsync(int idCliente)
        {
            string query = $@"
                SELECT {Columnas}
                FROM cliente
                WHERE IdCliente = @idCliente
                LIMIT 1";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@idCliente", idCliente);

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            return await reader.ReadAsync() ? MapearCliente(reader) : null;
        }

        private static Cliente MapearCliente(MySqlDataReader reader)
        {
            return new Cliente
            {
                IdCliente = reader.GetInt32("IdCliente"),
                Ci = reader["Ci"] == DBNull.Value ? "" : reader["Ci"].ToString() ?? "",
                Nombre = reader["Nombre"].ToString() ?? "",
                Apellido = reader["Apellido"].ToString() ?? "",
                Estado = reader.GetByte("Estado"),
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
