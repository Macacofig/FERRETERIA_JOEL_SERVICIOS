using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace ServicioVentas.Infraestructura.Repositorios
{
    /// <summary>
    /// Repositorio de productos usado por el módulo de ventas.
    /// Solo expone operaciones de lectura (obtener por id, listar activos)
    /// que necesita el flujo de venta. No incluye actualización ni cambio
    /// de estado (eso pertenece al módulo de productos).
    /// </summary>
    public class MySqlProductoRepository : IGestionarRepository<Producto>
    {
        private const string Columnas = @"
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

        public MySqlProductoRepository(IConexionTransaccional conexion)
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

        public virtual async Task<List<Producto>> ObtenerTodosAsync()
        {
            List<Producto> productos = new();

            string query = $@"
                SELECT {Columnas}
                FROM producto p
                LEFT JOIN marca m ON m.IdMarca = p.Marca
                ORDER BY p.Nombre ASC";

            using MySqlCommand command = await CrearComandoAsync(query);

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                productos.Add(MapearProducto(reader));
            }

            return productos;
        }

        public virtual async Task<int> InsertarAsync(Producto producto)
        {
            const string query = @"
                INSERT INTO producto
                (
                    IdCategoria,
                    Codigo,
                    Nombre,
                    Descripcion,
                    Marca,
                    UnidadMedida,
                    PrecioVenta,
                    CantidadStock,
                    Estado,
                    IdEmpleadoResponsable
                )
                VALUES
                (
                    @idCategoria,
                    @codigo,
                    @nombre,
                    @descripcion,
                    @idMarca,
                    @unidadMedida,
                    @precioVenta,
                    @cantidadStock,
                    1,
                    @idEmpleadoResponsable
                )";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@idCategoria", producto.IdCategoria);
            command.Parameters.AddWithValue("@codigo", producto.Codigo);
            command.Parameters.AddWithValue("@nombre", producto.Nombre);
            command.Parameters.AddWithValue(
                "@descripcion",
                (object?)producto.Descripcion ?? DBNull.Value);
            command.Parameters.AddWithValue("@idMarca", producto.IdMarca);
            command.Parameters.AddWithValue("@unidadMedida", producto.UnidadMedida);
            command.Parameters.AddWithValue("@precioVenta", producto.PrecioVenta);
            command.Parameters.AddWithValue("@cantidadStock", producto.CantidadStock);
            command.Parameters.AddWithValue(
                "@idEmpleadoResponsable",
                producto.IdEmpleadoResponsable);

            await command.ExecuteNonQueryAsync();

            return Convert.ToInt32(command.LastInsertedId);
        }

        public virtual async Task ActualizarAsync(Producto producto)
        {
            const string query = @"
                UPDATE producto
                SET
                    IdCategoria = @idCategoria,
                    Codigo = @codigo,
                    Nombre = @nombre,
                    Descripcion = @descripcion,
                    Marca = @idMarca,
                    UnidadMedida = @unidadMedida,
                    PrecioVenta = @precioVenta,
                    CantidadStock = @cantidadStock,
                    IdEmpleadoResponsable = @idEmpleadoResponsable,
                    Estado = @estado,
                    FechaActualizacion = CURRENT_TIMESTAMP
                WHERE IdProducto = @idProducto";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@idProducto", producto.IdProducto);
            command.Parameters.AddWithValue("@idCategoria", producto.IdCategoria);
            command.Parameters.AddWithValue("@codigo", producto.Codigo);
            command.Parameters.AddWithValue("@nombre", producto.Nombre);
            command.Parameters.AddWithValue(
                "@descripcion",
                (object?)producto.Descripcion ?? DBNull.Value);
            command.Parameters.AddWithValue("@idMarca", producto.IdMarca);
            command.Parameters.AddWithValue("@unidadMedida", producto.UnidadMedida);
            command.Parameters.AddWithValue("@precioVenta", producto.PrecioVenta);
            command.Parameters.AddWithValue("@cantidadStock", producto.CantidadStock);
            command.Parameters.AddWithValue(
                "@idEmpleadoResponsable",
                producto.IdEmpleadoResponsable);
            command.Parameters.AddWithValue("@estado", producto.Estado);

            await command.ExecuteNonQueryAsync();
        }

        public virtual async Task CambiarEstadoAsync(
            int idProducto,
            int idEmpleadoResponsable)
        {
            const string query = @"
                UPDATE producto
                SET
                    Estado = CASE
                        WHEN Estado = 1 THEN 0
                        ELSE 1
                    END,
                    FechaActualizacion = CURRENT_TIMESTAMP,
                    IdEmpleadoResponsable = @idEmpleadoResponsable
                WHERE IdProducto = @idProducto";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@idProducto", idProducto);
            command.Parameters.AddWithValue(
                "@idEmpleadoResponsable",
                idEmpleadoResponsable);

            await command.ExecuteNonQueryAsync();
        }

        public virtual async Task<List<Producto>> MostrarActivosAsync()
        {
            List<Producto> productos = new();

            string query = $@"
                SELECT {Columnas}
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

        public virtual async Task<Producto?> ObtenerPorIdAsync(int idProducto)
        {
            string query = $@"
                SELECT {Columnas}
                FROM producto p
                LEFT JOIN marca m ON m.IdMarca = p.Marca
                WHERE p.IdProducto = @idProducto";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@idProducto", idProducto);

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            return await reader.ReadAsync() ? MapearProducto(reader) : null;
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
