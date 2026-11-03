using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace FERRETERIA__Joel.Infraestructura.Repositorios
{
    /// <summary>
    /// Repositorio MySQL de la bitÃ¡cora. Registra dos familias de eventos en la
    /// misma tabla <c>bitacora_sesion</c>:
    /// <list type="bullet">
    /// <item>sesiÃ³n: login y logout;</item>
    /// <item>operaciones: INSERT, UPDATE y DELETE de los mÃ³dulos, con la
    /// entidad, el registro afectado y el resultado.</item>
    /// </list>
    /// Nunca se guardan contraseÃ±as, hashes, JWT ni tokens de activaciÃ³n.
    /// </summary>
    public class MySqlBitacoraRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MySqlBitacoraRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// InserciÃ³n usada por los eventos de sesiÃ³n. Mantiene la firma
        /// original para no romper el flujo existente.
        /// </summary>
        public virtual async Task<int> RegistrarAsync(
            string accion,
            int? idUsuario,
            string? nombreUsuario,
            string tipoUsuario)
        {
            return await RegistrarOperacionAsync(
                accion,
                null,
                null,
                "EXITOSO",
                idUsuario,
                nombreUsuario,
                tipoUsuario);
        }

        /// <summary>
        /// Inserta una operaciÃ³n CRUD (o un evento de sesiÃ³n) asociada al
        /// empleado autenticado que la ejecutÃ³.
        /// </summary>
        public virtual async Task<int> RegistrarOperacionAsync(
            string accion,
            string? entidad,
            int? idRegistroAfectado,
            string resultado,
            int? idUsuario,
            string? nombreUsuario,
            string tipoUsuario)
        {
            const string query = @"
                INSERT INTO bitacora_sesion
                (
                    Accion,
                    Entidad,
                    IdRegistroAfectado,
                    Resultado,
                    IdUsuario,
                    NombreUsuario,
                    TipoUsuario,
                    FechaHora
                )
                VALUES
                (
                    @accion,
                    @entidad,
                    @idRegistroAfectado,
                    @resultado,
                    @idUsuario,
                    @nombreUsuario,
                    @tipoUsuario,
                    CURRENT_TIMESTAMP
                )";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command =
                new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@accion", accion);

            command.Parameters.AddWithValue(
                "@entidad",
                (object?)entidad ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@idRegistroAfectado",
                (object?)idRegistroAfectado ?? DBNull.Value);

            command.Parameters.AddWithValue("@resultado", resultado);

            command.Parameters.AddWithValue(
                "@idUsuario",
                (object?)idUsuario ?? DBNull.Value);

            command.Parameters.AddWithValue(
                "@nombreUsuario",
                (object?)nombreUsuario ?? DBNull.Value);

            command.Parameters.AddWithValue("@tipoUsuario", tipoUsuario);

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();

            return Convert.ToInt32(command.LastInsertedId);
        }

        /// <summary>
        /// Devuelve los Ãºltimos registros de la bitÃ¡cora, del mÃ¡s reciente al
        /// mÃ¡s antiguo. La consulta no proyecta credenciales porque la tabla no
        /// las almacena.
        /// </summary>
        public virtual async Task<List<RegistroBitacora>> ListarAsync(int limite)
        {
            List<RegistroBitacora> registros = new();

            const string query = @"
                SELECT
                    IdBitacora,
                    Accion,
                    IdUsuario,
                    NombreUsuario,
                    TipoUsuario,
                    FechaHora,
                    Entidad,
                    IdRegistroAfectado,
                    Resultado
                FROM bitacora_sesion
                ORDER BY FechaHora DESC, IdBitacora DESC
                LIMIT @limite";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@limite", limite);

            await connection.OpenAsync();

            using MySqlDataReader reader =
                (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                registros.Add(new RegistroBitacora
                {
                    IdBitacora = reader.GetInt32("IdBitacora"),
                    Accion = reader["Accion"].ToString() ?? string.Empty,
                    IdUsuario = reader["IdUsuario"] == DBNull.Value
                        ? null
                        : reader.GetInt32("IdUsuario"),
                    NombreUsuario = reader["NombreUsuario"] == DBNull.Value
                        ? null
                        : reader["NombreUsuario"].ToString(),
                    TipoUsuario = reader["TipoUsuario"].ToString()
                        ?? string.Empty,
                    FechaHora = reader.GetDateTime("FechaHora"),
                    Entidad = reader["Entidad"] == DBNull.Value
                        ? null
                        : reader["Entidad"].ToString(),
                    IdRegistroAfectado =
                        reader["IdRegistroAfectado"] == DBNull.Value
                            ? null
                            : reader.GetInt32("IdRegistroAfectado"),
                    Resultado = reader["Resultado"] == DBNull.Value
                        ? "EXITOSO"
                        : reader["Resultado"].ToString() ?? "EXITOSO"
                });
            }

            return registros;
        }
    }
}

