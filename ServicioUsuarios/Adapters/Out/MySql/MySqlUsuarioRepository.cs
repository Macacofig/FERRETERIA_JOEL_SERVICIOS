using MySql.Data.MySqlClient;
using System.Data.Common;
using ServicioUsuarios.Application.Ports;
using ServicioUsuarios.Domain.Entities;

namespace ServicioUsuarios.Adapters.Out.MySql;

public sealed class MySqlUsuarioRepository(IMySqlConnectionFactory connectionFactory) : IUsuarioRepository
{
    private const string SelectColumns = """
        id, nombre_completo, nombre_usuario, correo, password_hash, rol,
        activo, debe_cambiar_password, creado_por, fecha_creacion,
        modificado_por, fecha_modificacion, dado_de_baja_por, fecha_baja
        """;

    public async Task<Usuario?> BuscarPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT id, nombre_completo, nombre_usuario, correo, password_hash, rol,
                   activo, debe_cambiar_password, creado_por, fecha_creacion,
                   modificado_por, fecha_modificacion, dado_de_baja_por, fecha_baja
            FROM usuario
            WHERE nombre_usuario = @nombreUsuario AND fecha_baja IS NULL
            LIMIT 1
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new Usuario
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            NombreCompleto = reader.GetString(reader.GetOrdinal("nombre_completo")),
            NombreUsuario = reader.GetString(reader.GetOrdinal("nombre_usuario")),
            Correo = reader.GetString(reader.GetOrdinal("correo")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            Rol = reader.GetString(reader.GetOrdinal("rol")),
            Activo = reader.GetBoolean(reader.GetOrdinal("activo")),
            DebeCambiarPassword = reader.GetBoolean(reader.GetOrdinal("debe_cambiar_password")),
            CreadoPor = reader.IsDBNull(reader.GetOrdinal("creado_por")) ? null : reader.GetString(reader.GetOrdinal("creado_por")),
            FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
            ModificadoPor = reader.IsDBNull(reader.GetOrdinal("modificado_por")) ? null : reader.GetString(reader.GetOrdinal("modificado_por")),
            FechaModificacion = reader.IsDBNull(reader.GetOrdinal("fecha_modificacion")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_modificacion")),
            DadoDeBajaPor = reader.IsDBNull(reader.GetOrdinal("dado_de_baja_por")) ? null : reader.GetString(reader.GetOrdinal("dado_de_baja_por")),
            FechaBaja = reader.IsDBNull(reader.GetOrdinal("fecha_baja")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_baja"))
        };
    }

    public async Task<List<Usuario>> ListarAsync(bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        string sql = $"SELECT {SelectColumns} FROM usuario WHERE fecha_baja IS NULL";
        if (!incluirInactivos)
            sql += " AND activo = TRUE";
        sql += " ORDER BY nombre_completo";

        await using var command = new MySqlCommand(sql, connection);
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var usuarios = new List<Usuario>();
        while (await reader.ReadAsync(cancellationToken))
            usuarios.Add(Map(reader));
        return usuarios;
    }

    public async Task<Usuario?> ObtenerPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand($"SELECT {SelectColumns} FROM usuario WHERE id = @id AND fecha_baja IS NULL LIMIT 1", connection);
        command.Parameters.AddWithValue("@id", id);
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT EXISTS(SELECT 1 FROM usuario WHERE nombre_usuario = @nombreUsuario)", connection);
        command.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);
        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<long> CrearAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            INSERT INTO usuario (nombre_completo, nombre_usuario, correo, password_hash, rol, activo,
                debe_cambiar_password, creado_por, fecha_creacion)
            VALUES (@nombreCompleto, @nombreUsuario, @correo, @passwordHash, @rol, FALSE, TRUE, @creadoPor, UTC_TIMESTAMP())
            """;
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@nombreCompleto", usuario.NombreCompleto);
        command.Parameters.AddWithValue("@nombreUsuario", usuario.NombreUsuario);
        command.Parameters.AddWithValue("@correo", usuario.Correo);
        command.Parameters.AddWithValue("@passwordHash", usuario.PasswordHash);
        command.Parameters.AddWithValue("@rol", usuario.Rol);
        command.Parameters.AddWithValue("@creadoPor", usuario.CreadoPor ?? "sistema");
        await command.ExecuteNonQueryAsync(cancellationToken);
        return command.LastInsertedId;
    }

    public async Task<bool> EditarAsync(long id, string nombreCompleto, string correo, string rol, string modificadoPor, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            UPDATE usuario SET nombre_completo=@nombreCompleto, correo=@correo, rol=@rol,
                modificado_por=@modificadoPor, fecha_modificacion=UTC_TIMESTAMP()
            WHERE id=@id AND fecha_baja IS NULL
            """;
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@nombreCompleto", nombreCompleto);
        command.Parameters.AddWithValue("@correo", correo);
        command.Parameters.AddWithValue("@rol", rol);
        command.Parameters.AddWithValue("@modificadoPor", modificadoPor);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> BajaLogicaAsync(long id, string dadoDeBajaPor, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            UPDATE usuario SET activo=FALSE, dado_de_baja_por=@dadoDeBajaPor,
                fecha_baja=UTC_TIMESTAMP(), modificado_por=@dadoDeBajaPor, fecha_modificacion=UTC_TIMESTAMP()
            WHERE id=@id AND fecha_baja IS NULL
            """;
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@dadoDeBajaPor", dadoDeBajaPor);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task GuardarTokenActivacionAsync(long usuarioId, string tokenHash, DateTime vencimiento, string creadoPor, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            INSERT INTO token_activacion (usuario_id, token_hash, fecha_expiracion, usado, creado_por, fecha_creacion)
            VALUES (@usuarioId, @tokenHash, @vencimiento, FALSE, @creadoPor, UTC_TIMESTAMP())
            """;
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@usuarioId", usuarioId);
        command.Parameters.AddWithValue("@tokenHash", tokenHash);
        command.Parameters.AddWithValue("@vencimiento", vencimiento);
        command.Parameters.AddWithValue("@creadoPor", creadoPor);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<long?> BuscarUsuarioPorTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT usuario_id FROM token_activacion
            WHERE token_hash=@tokenHash AND usado=FALSE AND fecha_expiracion > UTC_TIMESTAMP()
            LIMIT 1
            """;
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tokenHash", tokenHash);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null ? null : Convert.ToInt64(result);
    }

    public async Task<bool> ActivarAsync(long usuarioId, string tokenHash, string passwordHash, string modificadoPor, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var updateToken = new MySqlCommand("""
            UPDATE token_activacion SET usado=TRUE
            WHERE usuario_id=@usuarioId AND token_hash=@tokenHash AND usado=FALSE AND fecha_expiracion > UTC_TIMESTAMP()
            """, connection, transaction);
        updateToken.Parameters.AddWithValue("@usuarioId", usuarioId);
        updateToken.Parameters.AddWithValue("@tokenHash", tokenHash);
        if (await updateToken.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
        await using var updateUser = new MySqlCommand("""
            UPDATE usuario SET password_hash=@passwordHash, activo=TRUE, debe_cambiar_password=FALSE,
                modificado_por=@modificadoPor, fecha_modificacion=UTC_TIMESTAMP()
            WHERE id=@usuarioId AND fecha_baja IS NULL
            """, connection, transaction);
        updateUser.Parameters.AddWithValue("@passwordHash", passwordHash);
        updateUser.Parameters.AddWithValue("@usuarioId", usuarioId);
        updateUser.Parameters.AddWithValue("@modificadoPor", modificadoPor);
        bool activated = await updateUser.ExecuteNonQueryAsync(cancellationToken) == 1;
        if (activated) await transaction.CommitAsync(cancellationToken);
        else await transaction.RollbackAsync(cancellationToken);
        return activated;
    }

    public async Task<bool> CambiarPasswordAsync(long usuarioId, string passwordHash, string modificadoPor, CancellationToken cancellationToken = default)
    {
        await using MySqlConnection connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            UPDATE usuario
            SET password_hash=@passwordHash,
                debe_cambiar_password=FALSE,
                modificado_por=@modificadoPor,
                fecha_modificacion=UTC_TIMESTAMP()
            WHERE id=@usuarioId AND fecha_baja IS NULL
            """;
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        command.Parameters.AddWithValue("@modificadoPor", modificadoPor);
        command.Parameters.AddWithValue("@usuarioId", usuarioId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static Usuario Map(DbDataReader reader) => new()
    {
        Id = reader.GetInt64(reader.GetOrdinal("id")),
        NombreCompleto = reader.GetString(reader.GetOrdinal("nombre_completo")),
        NombreUsuario = reader.GetString(reader.GetOrdinal("nombre_usuario")),
        Correo = reader.GetString(reader.GetOrdinal("correo")),
        PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
        Rol = reader.GetString(reader.GetOrdinal("rol")),
        Activo = reader.GetBoolean(reader.GetOrdinal("activo")),
        DebeCambiarPassword = reader.GetBoolean(reader.GetOrdinal("debe_cambiar_password")),
        CreadoPor = reader.IsDBNull(reader.GetOrdinal("creado_por")) ? null : reader.GetString(reader.GetOrdinal("creado_por")),
        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
        ModificadoPor = reader.IsDBNull(reader.GetOrdinal("modificado_por")) ? null : reader.GetString(reader.GetOrdinal("modificado_por")),
        FechaModificacion = reader.IsDBNull(reader.GetOrdinal("fecha_modificacion")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_modificacion")),
        DadoDeBajaPor = reader.IsDBNull(reader.GetOrdinal("dado_de_baja_por")) ? null : reader.GetString(reader.GetOrdinal("dado_de_baja_por")),
        FechaBaja = reader.IsDBNull(reader.GetOrdinal("fecha_baja")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_baja"))
    };
}
