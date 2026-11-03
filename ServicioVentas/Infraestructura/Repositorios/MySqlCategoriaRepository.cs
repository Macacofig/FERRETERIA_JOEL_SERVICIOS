using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace ServicioVentas.Infraestructura.Repositorios
{
    public class MySqlCategoriaRepository : IGestionarRepository<Categoria>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public MySqlCategoriaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public virtual async Task<List<Categoria>> ObtenerTodosAsync()
        {
            List<Categoria> categorias = new();

            const string query = @"
                SELECT
                    IdCategoria,
                    Codigo,
                    Nombre,
                    Descripcion,
                    PorcentajeGanancia,
                    Estado,
                    FechaRegistro,
                    FechaActualizacion,
                    IdEmpleadoResponsable
                FROM categoria
                ORDER BY Nombre ASC";

            using MySqlConnection connection = _connectionFactory.CreateConnection();
            using MySqlCommand command = new MySqlCommand(query, connection);

            await connection.OpenAsync();

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categorias.Add(MapearCategoria(reader));
            }

            return categorias;
        }

        public virtual async Task<int> InsertarAsync(Categoria categoria)
        {
            const string query = @"INSERT INTO categoria 
                                   (Codigo, Nombre, Descripcion, PorcentajeGanancia, Estado, IdEmpleadoResponsable) 
                                   VALUES 
                                   (@Codigo, @Nombre, @Descripcion, @PorcentajeGanancia, @Estado, @IdEmpleadoResponsable);";

            using MySqlConnection connection = _connectionFactory.CreateConnection();
            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@Codigo", categoria.Codigo.Trim().ToUpper());
            command.Parameters.AddWithValue("@Nombre", categoria.Nombre.Trim());
            command.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(categoria.Descripcion) ? (object)DBNull.Value : categoria.Descripcion.Trim());
            command.Parameters.AddWithValue("@PorcentajeGanancia", categoria.PorcentajeGanancia);
            command.Parameters.AddWithValue("@Estado", categoria.Estado);
            command.Parameters.AddWithValue("@IdEmpleadoResponsable", categoria.IdEmpleadoResponsable);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return Convert.ToInt32(command.LastInsertedId);
        }

        public virtual async Task ActualizarAsync(Categoria categoria)
        {
            const string query = @"UPDATE categoria 
                                   SET Codigo = @Codigo, 
                                       Nombre = @Nombre, 
                                       Descripcion = @Descripcion, 
                                       PorcentajeGanancia = @PorcentajeGanancia,
                                       Estado = @Estado, 
                                       FechaActualizacion = CURRENT_TIMESTAMP,
                                       IdEmpleadoResponsable = @IdEmpleadoResponsable 
                                   WHERE IdCategoria = @IdCategoria;";

            using MySqlConnection connection = _connectionFactory.CreateConnection();
            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@Codigo", categoria.Codigo.Trim().ToUpper());
            command.Parameters.AddWithValue("@Nombre", categoria.Nombre.Trim());
            command.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(categoria.Descripcion) ? (object)DBNull.Value : categoria.Descripcion.Trim());
            command.Parameters.AddWithValue("@PorcentajeGanancia", categoria.PorcentajeGanancia);
            command.Parameters.AddWithValue("@Estado", categoria.Estado);
            command.Parameters.AddWithValue("@IdEmpleadoResponsable", categoria.IdEmpleadoResponsable);
            command.Parameters.AddWithValue("@IdCategoria", categoria.IdCategoria);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public virtual async Task CambiarEstadoAsync(int id, int idEmpleadoResponsable)
        {
            const string query = @"UPDATE categoria 
                                   SET Estado = CASE
                                       WHEN Estado = 1 THEN 0
                                       ELSE 1
                                   END,
                                       FechaActualizacion = CURRENT_TIMESTAMP,
                                       IdEmpleadoResponsable = @IdEmpleadoResponsable 
                                   WHERE IdCategoria = @IdCategoria;";

            using MySqlConnection connection = _connectionFactory.CreateConnection();
            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@IdCategoria", id);
            command.Parameters.AddWithValue(
                "@IdEmpleadoResponsable",
                idEmpleadoResponsable);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public virtual async Task<List<Categoria>> MostrarActivosAsync()
        {
            List<Categoria> categorias = new();

            const string query = @"
                SELECT
                    IdCategoria,
                    Codigo,
                    Nombre,
                    Descripcion,
                    PorcentajeGanancia,
                    Estado,
                    FechaRegistro,
                    FechaActualizacion,
                    IdEmpleadoResponsable
                FROM categoria
                WHERE Estado = 1
                ORDER BY Nombre ASC";

            using MySqlConnection connection = _connectionFactory.CreateConnection();
            using MySqlCommand command = new MySqlCommand(query, connection);

            await connection.OpenAsync();

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categorias.Add(MapearCategoria(reader));
            }

            return categorias;
        }

        public virtual async Task<Categoria?> ObtenerPorIdAsync(int id)
        {
            const string query = @"
                SELECT
                    IdCategoria,
                    Codigo,
                    Nombre,
                    Descripcion,
                    PorcentajeGanancia,
                    Estado,
                    FechaRegistro,
                    FechaActualizacion,
                    IdEmpleadoResponsable
                FROM categoria
                WHERE IdCategoria = @IdCategoria
                LIMIT 1";

            using MySqlConnection connection = _connectionFactory.CreateConnection();
            using MySqlCommand command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@IdCategoria",
                id);

            await connection.OpenAsync();

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? MapearCategoria(reader)
                : null;
        }

        public virtual async Task<int> CountAsync()
        {
            const string query = @"
                SELECT COUNT(*)
                FROM categoria";

            using MySqlConnection connection = _connectionFactory.CreateConnection();
            using MySqlCommand command = new MySqlCommand(query, connection);

            await connection.OpenAsync();

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private Categoria MapearCategoria(MySqlDataReader reader)
        {
            return new Categoria
            {
                IdCategoria = reader.GetInt32("IdCategoria"),
                Codigo = reader["Codigo"].ToString() ?? "",
                Nombre = reader["Nombre"].ToString() ?? "",
                Descripcion = reader["Descripcion"] == DBNull.Value
                    ? null
                    : reader["Descripcion"].ToString(),
                PorcentajeGanancia = reader.GetDecimal("PorcentajeGanancia"),
                Estado = reader.GetByte("Estado"),
                FechaRegistro = reader.GetDateTime("FechaRegistro"),
                FechaActualizacion = reader["FechaActualizacion"] == DBNull.Value
                    ? null
                    : reader.GetDateTime("FechaActualizacion"),
                IdEmpleadoResponsable = reader.GetInt32("IdEmpleadoResponsable")
            };
        }
        public virtual async Task IncrementarPorcentajeGananciaAsync(int idCategoria = 0, decimal valor = 0m)
        {
            const string query = @"
            UPDATE categoria c
            LEFT JOIN (
                SELECT 
                    p.IdCategoria,
                    SUM(dv.TotalProducto) AS TotalCategoria
                FROM detalle_venta dv
                INNER JOIN venta v ON v.IdVenta = dv.IdVenta
                INNER JOIN producto p ON p.IdProducto = dv.IdProducto
                WHERE v.EstadoVenta = 'Registrada'
                GROUP BY p.IdCategoria
            ) sub ON c.IdCategoria = sub.IdCategoria
            CROSS JOIN (
                SELECT COALESCE(SUM(dv.TotalProducto), 0) AS GranTotal
                FROM detalle_venta dv
                INNER JOIN venta v ON v.IdVenta = dv.IdVenta
                WHERE v.EstadoVenta = 'Registrada'
            ) total
            SET c.PorcentajeGanancia = CASE 
                WHEN total.GranTotal > 0 THEN ROUND((COALESCE(sub.TotalCategoria, 0) / total.GranTotal) * 100, 2)
                ELSE 0.00
            END,
            c.FechaActualizacion = CURRENT_TIMESTAMP;";

            using MySqlConnection connection = _connectionFactory.CreateConnection();
            using MySqlCommand command = new MySqlCommand(query, connection);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}
