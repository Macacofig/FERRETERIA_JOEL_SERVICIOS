using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace FERRETERIA__Joel.Infraestructura.Repositorios
{
    /// <summary>
    /// Repositorio de empleados. Implementa el contrato genérico
    /// <see cref="IRepository{T}"/> y el contrato propio
    /// <see cref="IRepositoryEmpleado"/> (baja lógica y proyecciones sin
    /// credenciales).
    ///
    /// <para>
    /// Las consultas están divididas en tres proyecciones para que el hash de
    /// la contraseña y el token de activación nunca travelen a las vistas ni a
    /// los listados: solo se piden cuando el proceso de autenticación o de
    /// activación los necesita de verdad.
    /// </para>
    /// </summary>
    public class MySqlEmpleadoRepository : IRepositoryEmpleado
    {
        /// <summary>
        /// Proyección pública: datos de la cuenta que puede ver la interfaz.
        /// No incluye la contraseña ni el token de activación.
        /// </summary>
        private const string ColumnasPublicas = @"
            IdEmpleado,
            Nombre,
            Apellido,
            NombreUsuario,
            Rol,
            Telefono,
            CorreoElectronico,
            Estado,
            FechaRegistro,
            FechaActualizacion,
            IdEmpleadoResponsable";

        /// <summary>
        /// Proyección del flujo de activación: incluye el token y su vigencia,
        /// pero no la contraseña.
        /// </summary>
        private const string ColumnasActivacion = @"
            IdEmpleado,
            Nombre,
            Apellido,
            NombreUsuario,
            Rol,
            Telefono,
            CorreoElectronico,
            TokenActivacion,
            TokenExpira,
            TokenUsado,
            Estado,
            FechaRegistro,
            FechaActualizacion,
            IdEmpleadoResponsable";

        /// <summary>
        /// Proyección de credenciales: se usa únicamente en el proceso de
        /// autenticación. El valor no debe salir de la capa de aplicación.
        /// </summary>
        private const string ColumnasCredenciales = @"
            IdEmpleado,
            Nombre,
            Apellido,
            NombreUsuario,
            Rol,
            Telefono,
            CorreoElectronico,
            Contrasena,
            TokenActivacion,
            TokenExpira,
            TokenUsado,
            Estado,
            FechaRegistro,
            FechaActualizacion,
            IdEmpleadoResponsable";

        private readonly IDbConnectionFactory _connectionFactory;

        public MySqlEmpleadoRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // ================== Contrato genérico ==================

        /// <summary>
        /// Lista el personal con la proyección pública: la contraseña y el token
        /// de activación no se consultan ni se devuelven.
        /// </summary>
        public virtual async Task<List<Empleado>> ObtenerTodosAsync()
        {
            return await ObtenerTodosSinCredencialesAsync();
        }

        public virtual async Task<int> InsertarAsync(Empleado empleado)
        {
            const string query = @"
                INSERT INTO empleado
                (
                    Nombre,
                    Apellido,
                    NombreUsuario,
                    Rol,
                    Telefono,
                    CorreoElectronico,
                    Contrasena,
                    TokenActivacion,
                    TokenExpira,
                    TokenUsado,
                    Estado,
                    IdEmpleadoResponsable
                )
                VALUES
                (
                    @nombre,
                    @apellido,
                    @nombreUsuario,
                    @rol,
                    @telefono,
                    @correoElectronico,
                    @contrasena,
                    @tokenActivacion,
                    @tokenExpira,
                    @tokenUsado,
                    @estado,
                    @idEmpleadoResponsable
                )";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@nombre", empleado.Nombre);
            command.Parameters.AddWithValue("@apellido", empleado.Apellido);
            command.Parameters.AddWithValue(
                "@nombreUsuario",
                (object?)empleado.NombreUsuario ?? DBNull.Value);
            command.Parameters.AddWithValue("@rol", empleado.Rol);
            command.Parameters.AddWithValue("@telefono", empleado.Telefono);
            command.Parameters.AddWithValue(
                "@correoElectronico",
                (object?)empleado.CorreoElectronico ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "@contrasena",
                (object?)empleado.Contrasena ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "@tokenActivacion",
                (object?)empleado.TokenActivacion ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "@tokenExpira",
                (object?)empleado.TokenExpira ?? DBNull.Value);
            command.Parameters.AddWithValue("@tokenUsado", empleado.TokenUsado);
            command.Parameters.AddWithValue("@estado", empleado.Estado);
            command.Parameters.AddWithValue(
                "@idEmpleadoResponsable",
                (object?)empleado.IdEmpleadoResponsable ?? DBNull.Value);

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();

            return Convert.ToInt32(command.LastInsertedId);
        }

        // ================== Proyecciones sin credenciales ==================

        /// <summary>
        /// Devuelve el personal sin la contraseña ni el token de activación.
        /// Es la proyección que se expone a la interfaz y a los listados.
        /// </summary>
        public virtual async Task<List<Empleado>> ObtenerTodosSinCredencialesAsync()
        {
            List<Empleado> empleados = new();

            string query = $@"
                SELECT {ColumnasPublicas}
                FROM empleado
                ORDER BY Nombre ASC";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            await connection.OpenAsync();

            using MySqlDataReader reader =
                (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                empleados.Add(MapearEmpleado(reader));
            }

            return empleados;
        }

        /// <summary>
        /// Devuelve una cuenta por su identificador con la proyección pública,
        /// sin la contraseña ni el token de activación. Se usa para revalidar
        /// que la cuenta siga activa en cada petición autenticada.
        /// </summary>
        public virtual async Task<Empleado?> ObtenerPorIdSinCredencialesAsync(
            int idEmpleado)
        {
            string query = $@"
                SELECT {ColumnasPublicas}
                FROM empleado
                WHERE IdEmpleado = @id
                LIMIT 1";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", idEmpleado);

            await connection.OpenAsync();

            using MySqlDataReader reader =
                (MySqlDataReader)await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? MapearEmpleado(reader)
                : null;
        }

        // ================== Autenticación ==================

        /// <summary>
        /// Busca la cuenta asociada a un correo con su hash de contraseña.
        /// Se reserva para el proceso de autenticación.
        /// </summary>
        public virtual async Task<Empleado?> ObtenerPorCorreoConCredencialesAsync(
            string correo)
        {
            string query = $@"
                SELECT {ColumnasCredenciales}
                FROM empleado
                WHERE CorreoElectronico = @correo
                LIMIT 1";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@correo", correo);

            await connection.OpenAsync();

            using MySqlDataReader reader =
                (MySqlDataReader)await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? MapearEmpleado(reader)
                : null;
        }

        /// <summary>
        /// Busca la cuenta por correo o por nombre de usuario con su hash de
        /// contraseña. Permite que el login siga aceptando el correo actual y,
        /// además, el nombre de usuario generado en el alta.
        /// </summary>
        public virtual async Task<Empleado?> ObtenerPorIdentificadorConCredencialesAsync(
            string identificador)
        {
            string query = $@"
                SELECT {ColumnasCredenciales}
                FROM empleado
                WHERE CorreoElectronico = @identificador
                   OR NombreUsuario = @identificador
                LIMIT 1";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@identificador", identificador);

            await connection.OpenAsync();

            using MySqlDataReader reader =
                (MySqlDataReader)await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? MapearEmpleado(reader)
                : null;
        }

        /// <summary>
        /// Devuelve una cuenta por identificador con su hash de contraseña.
        /// Solo se usa para verificar la contraseña actual en el cambio de
        /// contraseña: el hash nunca sale de la capa de aplicación.
        /// </summary>
        public virtual async Task<Empleado?> ObtenerPorIdConCredencialesAsync(
            int idEmpleado)
        {
            string query = $@"
                SELECT {ColumnasCredenciales}
                FROM empleado
                WHERE IdEmpleado = @id
                LIMIT 1";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", idEmpleado);

            await connection.OpenAsync();

            using MySqlDataReader reader =
                (MySqlDataReader)await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? MapearEmpleado(reader)
                : null;
        }

        /// <summary>
        /// Indica si un nombre de usuario ya existe. La consulta solo recupera
        /// el identificador, nunca credenciales.
        /// </summary>
        public virtual async Task<bool> ExistePorNombreUsuarioAsync(
            string nombreUsuario)
        {
            const string query = @"
                SELECT IdEmpleado
                FROM empleado
                WHERE NombreUsuario = @nombreUsuario
                LIMIT 1";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);

            await connection.OpenAsync();

            object? resultado = await command.ExecuteScalarAsync();

            return resultado is not null;
        }

        /// <summary>
        /// Busca la cuenta asociada a un correo con la proyección pública: sin
        /// contraseña ni token de activación.
        /// </summary>
        public virtual async Task<Empleado?> ObtenerPorCorreoSinCredencialesAsync(
            string correo)
        {
            string query = $@"
                SELECT {ColumnasPublicas}
                FROM empleado
                WHERE CorreoElectronico = @correo
                LIMIT 1";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@correo", correo);

            await connection.OpenAsync();

            using MySqlDataReader reader =
                (MySqlDataReader)await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? MapearEmpleado(reader)
                : null;
        }

        /// <summary>
        /// Indica si ya existe una cuenta con ese correo. La consulta solo
        /// recupera el identificador: no expone ninguna credencial.
        /// </summary>
        public virtual async Task<bool> ExistePorCorreoAsync(string correo)
        {
            const string query = @"
                SELECT IdEmpleado
                FROM empleado
                WHERE CorreoElectronico = @correo
                LIMIT 1";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@correo", correo);

            await connection.OpenAsync();

            object? resultado = await command.ExecuteScalarAsync();

            return resultado is not null;
        }

        /// <summary>
        /// Busca la cuenta asociada a un correo. Se mantiene por compatibilidad
        /// con los servicios que solo necesitan validar la existencia de la
        /// cuenta; delega en la consulta con credenciales.
        /// </summary>
        public virtual async Task<Empleado?> ObtenerPorCorreoAsync(string correo)
        {
            return await ObtenerPorCorreoConCredencialesAsync(correo);
        }

        public virtual async Task ActualizarContrasenaAsync(
            int id,
            string contrasena)
        {
            const string query = @"
                UPDATE empleado
                SET
                    Contrasena = @contrasena,
                    FechaActualizacion = CURRENT_TIMESTAMP
                WHERE IdEmpleado = @id";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@contrasena", contrasena);

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();
        }

        // ================== Edición de datos personales ==================

        /// <summary>
        /// Actualiza nombre, apellido, teléfono, correo y rol. No modifica la
        /// contraseña, el token de activación ni el estado: el cambio de
        /// contraseña tiene su propio flujo y la baja lógica su propia
        /// operación.
        /// </summary>
        public virtual async Task<bool> ActualizarDatosAsync(
            Empleado empleado,
            int idEmpleadoResponsable)
        {
            const string query = @"
                UPDATE empleado
                SET
                    Nombre = @nombre,
                    Apellido = @apellido,
                    Rol = @rol,
                    Telefono = @telefono,
                    CorreoElectronico = @correoElectronico,
                    FechaActualizacion = CURRENT_TIMESTAMP,
                    IdEmpleadoResponsable = @idEmpleadoResponsable
                WHERE IdEmpleado = @id";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", empleado.IdEmpleado);
            command.Parameters.AddWithValue("@nombre", empleado.Nombre);
            command.Parameters.AddWithValue("@apellido", empleado.Apellido);
            command.Parameters.AddWithValue("@rol", empleado.Rol);
            command.Parameters.AddWithValue("@telefono", empleado.Telefono);
            command.Parameters.AddWithValue(
                "@correoElectronico",
                (object?)empleado.CorreoElectronico ?? DBNull.Value);
            command.Parameters.AddWithValue(
                "@idEmpleadoResponsable",
                idEmpleadoResponsable);

            await connection.OpenAsync();

            int filas = await command.ExecuteNonQueryAsync();

            return filas > 0;
        }

        // ================== Baja lógica ==================

        /// <summary>
        /// Fija el estado de la cuenta sin borrado físico. Es la operación de
        /// baja lógica (cuenta dada de baja por el administrador) y de
        /// reactivación. Devuelve <c>false</c> si la cuenta no existe o si ya
        /// se encuentra en el estado solicitado.
        /// </summary>
        public virtual async Task<bool> ActualizarEstadoAsync(
            int idEmpleado,
            sbyte nuevoEstado,
            int idEmpleadoResponsable)
        {
            const string query = @"
                UPDATE empleado
                SET
                    Estado = @estado,
                    FechaActualizacion = CURRENT_TIMESTAMP,
                    IdEmpleadoResponsable = @idEmpleadoResponsable
                WHERE IdEmpleado = @id
                  AND Estado <> @estado";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", idEmpleado);
            command.Parameters.AddWithValue("@estado", nuevoEstado);
            command.Parameters.AddWithValue(
                "@idEmpleadoResponsable",
                idEmpleadoResponsable);

            await connection.OpenAsync();

            int filas = await command.ExecuteNonQueryAsync();

            return filas > 0;
        }

        // ================== Activación por correo ==================

        /// <summary>
        /// Busca al empleado dueño de un token de activación, sin importar si
        /// ya venció o se usó: la capa de aplicación decide si es válido.
        /// </summary>
        public virtual async Task<Empleado?> ObtenerPorTokenActivacionAsync(
            string token)
        {
            string query = $@"
                SELECT {ColumnasActivacion}
                FROM empleado
                WHERE TokenActivacion = @token
                LIMIT 1";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@token", token);

            await connection.OpenAsync();

            using MySqlDataReader reader =
                (MySqlDataReader)await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? MapearEmpleado(reader)
                : null;
        }

        /// <summary>
        /// Empleados pendientes de activación (Estado -1). Se usan para
        /// listarlos y permitir reenviarles el enlace cuando el token venció.
        /// </summary>
        public virtual async Task<List<Empleado>> ObtenerPendientesActivacionAsync()
        {
            List<Empleado> empleados = new();

            string query = $@"
                SELECT {ColumnasActivacion}
                FROM empleado
                WHERE Estado = @estado
                ORDER BY FechaRegistro DESC";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@estado",
                EstadoEmpleado.PendienteActivacion);

            await connection.OpenAsync();

            using MySqlDataReader reader =
                (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                empleados.Add(MapearEmpleado(reader));
            }

            return empleados;
        }

        /// <summary>
        /// Reemplaza el token de activación de un empleado pendiente y reinicia
        /// su vigencia. Se usa al reenviar el enlace de activación.
        /// </summary>
        public virtual async Task ReasignarTokenActivacionAsync(
            int id,
            string token,
            DateTime expira)
        {
            const string query = @"
                UPDATE empleado
                SET
                    TokenActivacion = @token,
                    TokenExpira = @expira,
                    TokenUsado = 0,
                    FechaActualizacion = CURRENT_TIMESTAMP
                WHERE IdEmpleado = @id";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@token", token);
            command.Parameters.AddWithValue("@expira", expira);

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Activa la cuenta: guarda la contraseña definida por el empleado,
        /// marca el token como usado y pasa el Estado a activo (1).
        /// </summary>
        public virtual async Task ActivarCuentaAsync(
            int id,
            string contrasena,
            sbyte estadoActivo)
        {
            const string query = @"
                UPDATE empleado
                SET
                    Contrasena = @contrasena,
                    TokenUsado = 1,
                    Estado = @estado,
                    FechaActualizacion = CURRENT_TIMESTAMP
                WHERE IdEmpleado = @id";

            using MySqlConnection connection =
                _connectionFactory.CreateConnection();

            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@contrasena", contrasena);
            command.Parameters.AddWithValue("@estado", estadoActivo);

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Mapea la fila leída. Las columnas que no trae la proyección
        /// (contraseña y token) quedan en <c>null</c> o en su valor por
        /// defecto, según el caso.
        /// </summary>
        private static Empleado MapearEmpleado(MySqlDataReader reader)
        {
            return new Empleado
            {
                IdEmpleado = reader.GetInt32("IdEmpleado"),
                Nombre = reader["Nombre"].ToString() ?? "",
                Apellido = reader["Apellido"].ToString() ?? "",
                NombreUsuario = ObtenerTexto(reader, "NombreUsuario"),
                Rol = reader["Rol"].ToString() ?? "",
                Telefono = reader["Telefono"].ToString() ?? "",
                CorreoElectronico = reader["CorreoElectronico"] == DBNull.Value
                    ? null
                    : reader["CorreoElectronico"].ToString(),
                Contrasena = ObtenerTexto(reader, "Contrasena"),
                TokenActivacion = ObtenerTexto(reader, "TokenActivacion"),
                TokenExpira = ObtenerFecha(reader, "TokenExpira"),
                TokenUsado = ObtenerLogico(reader, "TokenUsado"),
                Estado = reader.GetSByte("Estado"),
                FechaRegistro = reader.GetDateTime("FechaRegistro"),
                FechaActualizacion = reader["FechaActualizacion"] == DBNull.Value
                    ? null
                    : reader.GetDateTime("FechaActualizacion"),
                IdEmpleadoResponsable = reader["IdEmpleadoResponsable"] == DBNull.Value
                    ? null
                    : reader.GetInt32("IdEmpleadoResponsable")
            };
        }

        private static string? ObtenerTexto(
            MySqlDataReader reader,
            string columna)
        {
            if (!TieneColumna(reader, columna))
            {
                return null;
            }

            int ordinal = reader.GetOrdinal(columna);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }

        private static bool ObtenerLogico(
            MySqlDataReader reader,
            string columna)
        {
            if (!TieneColumna(reader, columna))
            {
                return false;
            }

            int ordinal = reader.GetOrdinal(columna);

            return reader.IsDBNull(ordinal)
                ? false
                : Convert.ToBoolean(reader.GetValue(ordinal));
        }

        /// <summary>
        /// Lee una fecha que puede no estar en la proyección consultada. Por
        /// ejemplo, la proyección pública no trae la vigencia del token de
        /// activación: en ese caso el valor queda en <c>null</c> en lugar de
        /// provocar un error al leer la columna.
        /// </summary>
        private static DateTime? ObtenerFecha(
            MySqlDataReader reader,
            string columna)
        {
            if (!TieneColumna(reader, columna))
            {
                return null;
            }

            int ordinal = reader.GetOrdinal(columna);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetDateTime(ordinal);
        }

        /// <summary>
        /// Indica si la proyección consultada incluye la columna. Permite que
        /// el mismo mapeo sirva para la proyección pública (sin credenciales)
        /// y para la de credenciales.
        /// </summary>
        private static bool TieneColumna(
            MySqlDataReader reader,
            string columna)
        {
            try
            {
                _ = reader.GetOrdinal(columna);
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }
    }
}

