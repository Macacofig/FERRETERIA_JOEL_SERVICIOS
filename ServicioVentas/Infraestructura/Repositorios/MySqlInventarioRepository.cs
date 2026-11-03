using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace ServicioVentas.Infraestructura.Repositorios
{
    /// <summary>
    /// Repositorio de inventario: operaciones de stock sobre producto. No
    /// encaja en un contrato genérico CRUD, por lo que se inyecta como clase
    /// concreta en <c>FachadaActualizacionStock</c> (operación especial).
    /// </summary>
    public class MySqlInventarioRepository
    {
        private const string ColumnasProducto = @"
            p.IdProducto,
            p.IdCategoria,
            p.Codigo,
            p.Nombre,
            p.Descripcion,
            p.Marca AS IdMarca,
            m.Nombre AS NombreMarca,
            p.UnidadMedida,
            p.PrecioVenta,
            p.CantidadStock,
            p.Estado,
            p.FechaRegistro,
            p.FechaActualizacion,
            p.IdEmpleadoResponsable";

        private readonly IConexionTransaccional _conexion;

        public MySqlInventarioRepository(IConexionTransaccional conexion)
        {
            _conexion = conexion;
        }

        private async Task<MySqlCommand> CrearComandoAsync(string query)
        {
            MySqlConnection conexion = await _conexion.ObtenerConexionAsync();

            MySqlCommand command = new(query, conexion);

            if (_conexion.Transaccion is not null)
            {
                command.Transaction = _conexion.Transaccion;
            }

            return command;
        }

        public virtual async Task<Producto?> ObtenerParaVentaAsync(int idProducto)
        {
            string query = $@"
                SELECT {ColumnasProducto}
                FROM producto p
                LEFT JOIN marca m ON m.IdMarca = p.Marca
                WHERE p.IdProducto = @idProducto
                  AND p.Estado = 1
                LIMIT 1";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@idProducto", idProducto);

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapearProducto(reader);
        }

        public virtual async Task<bool> DescontarAsync(int idProducto, int cantidad)
        {
            const string query = @"
                UPDATE producto
                SET
                    CantidadStock = CantidadStock - @cantidad,
                    FechaActualizacion = CURRENT_TIMESTAMP
                WHERE IdProducto = @idProducto
                  AND Estado = 1
                  AND CantidadStock >= @cantidad";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@idProducto", idProducto);
            command.Parameters.AddWithValue("@cantidad", cantidad);

            return await command.ExecuteNonQueryAsync() == 1;
        }

        public virtual async Task ReponerAsync(int idProducto, int cantidad)
        {
            const string query = @"
                UPDATE producto
                SET
                    CantidadStock = CantidadStock + @cantidad,
                    FechaActualizacion = CURRENT_TIMESTAMP
                WHERE IdProducto = @idProducto";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@idProducto", idProducto);
            command.Parameters.AddWithValue("@cantidad", cantidad);

            await command.ExecuteNonQueryAsync();
        }

        public virtual async Task<List<Producto>> ListarActivosAsync()
        {
            List<Producto> productos = new();

            string query = $@"
                SELECT {ColumnasProducto}
                FROM producto p
                LEFT JOIN marca m ON m.IdMarca = p.Marca
                WHERE p.Estado = 1
                ORDER BY p.Nombre ASC";

            using MySqlCommand command = await CrearComandoAsync(query);

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                productos.Add(MapearProducto(reader));
            }

            return productos;
        }

        public virtual async Task<List<Producto>> ObtenerPorIdsAsync(
            IReadOnlyCollection<int> ids)
        {
            List<Producto> productos = new();

            if (ids is null || ids.Count == 0)
            {
                return productos;
            }

            List<int> distintos = ids.Distinct().ToList();
            List<string> parametros = new();

            for (int i = 0; i < distintos.Count; i++)
            {
                parametros.Add($"@id{i}");
            }

            string query = $@"
                SELECT {ColumnasProducto}
                FROM producto p
                LEFT JOIN marca m ON m.IdMarca = p.Marca
                WHERE p.IdProducto IN ({string.Join(", ", parametros)})";

            using MySqlCommand command = await CrearComandoAsync(query);

            for (int i = 0; i < distintos.Count; i++)
            {
                command.Parameters.AddWithValue($"@id{i}", distintos[i]);
            }

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                productos.Add(MapearProducto(reader));
            }

            return productos;
        }

        private static Producto MapearProducto(MySqlDataReader reader)
        {
            return new Producto
            {
                IdProducto = reader.GetInt32("IdProducto"),
                IdCategoria = reader.GetInt32("IdCategoria"),
                IdMarca = reader.GetInt32("IdMarca"),
                Codigo = reader["Codigo"].ToString() ?? "",
                Nombre = reader["Nombre"].ToString() ?? "",
                Descripcion = reader["Descripcion"] == DBNull.Value
                    ? null
                    : reader["Descripcion"].ToString(),
                NombreMarca = reader["NombreMarca"] == DBNull.Value
                    ? null
                    : reader["NombreMarca"].ToString(),
                UnidadMedida = reader["UnidadMedida"].ToString() ?? "",
                PrecioVenta = reader.GetDecimal("PrecioVenta"),
                CantidadStock = reader.GetInt32("CantidadStock"),
                Estado = reader.GetByte("Estado"),
                FechaRegistro = reader.GetDateTime("FechaRegistro"),
                FechaActualizacion = reader["FechaActualizacion"] == DBNull.Value
                    ? null
                    : reader.GetDateTime("FechaActualizacion"),
                IdEmpleadoResponsable = reader.GetInt32("IdEmpleadoResponsable")
            };
        }
    }
}
