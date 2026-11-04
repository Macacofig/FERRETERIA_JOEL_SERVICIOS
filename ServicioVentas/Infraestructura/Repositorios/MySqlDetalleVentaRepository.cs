using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using MySql.Data.MySqlClient;

namespace ServicioVentas.Infraestructura.Repositorios
{
    /// <summary>
    /// Repositorio de detalle de venta. Implementa el contrato genérico
    /// <see cref="IRepository{T}"/> y expone la consulta por venta como método
    /// concreto inyectado por DI. Participa de la transacción ambiente.
    /// </summary>
    public class MySqlDetalleVentaRepository : IDetalleVentaRepository
    {
        private const string Columnas = @"
            IdDetalleVenta,
            IdVenta,
            IdProducto,
            Cantidad,
            PrecioUnitario,
            TotalProducto,
            FechaRegistro,
            IdEmpleadoResponsable";

        private readonly IConexionTransaccional _conexion;

        public MySqlDetalleVentaRepository(IConexionTransaccional conexion)
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

        public virtual async Task<List<DetalleVenta>> ObtenerTodosAsync()
        {
            List<DetalleVenta> detalles = new();

            string query = $@"
                SELECT {Columnas}
                FROM detalle_venta
                ORDER BY IdDetalleVenta ASC";

            using MySqlCommand command = await CrearComandoAsync(query);

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                detalles.Add(MapearDetalleVenta(reader));
            }

            return detalles;
        }

        public virtual async Task<int> InsertarAsync(DetalleVenta detalleVenta)
        {
            const string query = @"
                INSERT INTO detalle_venta
                (
                    IdVenta,
                    IdProducto,
                    Cantidad,
                    PrecioUnitario,
                    TotalProducto,
                    IdEmpleadoResponsable
                )
                VALUES
                (
                    @idVenta,
                    @idProducto,
                    @cantidad,
                    @precioUnitario,
                    @totalProducto,
                    @idEmpleadoResponsable
                )";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@idVenta", detalleVenta.IdVenta);
            command.Parameters.AddWithValue("@idProducto", detalleVenta.IdProducto);
            command.Parameters.AddWithValue("@cantidad", detalleVenta.Cantidad);
            command.Parameters.AddWithValue("@precioUnitario", detalleVenta.PrecioUnitario);
            command.Parameters.AddWithValue("@totalProducto", detalleVenta.TotalProducto);
            command.Parameters.AddWithValue(
                "@idEmpleadoResponsable",
                detalleVenta.IdEmpleadoResponsable);

            await command.ExecuteNonQueryAsync();

            return Convert.ToInt32(command.LastInsertedId);
        }

        // ================== Operaciones propias de Ventas ==================

        public virtual async Task<List<DetalleVenta>> ObtenerPorVentaAsync(int idVenta)
        {
            List<DetalleVenta> detalles = new();

            string query = $@"
                SELECT {Columnas}
                FROM detalle_venta
                WHERE IdVenta = @idVenta
                ORDER BY IdDetalleVenta ASC";

            using MySqlCommand command = await CrearComandoAsync(query);

            command.Parameters.AddWithValue("@idVenta", idVenta);

            using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                detalles.Add(MapearDetalleVenta(reader));
            }

            return detalles;
        }

        private static DetalleVenta MapearDetalleVenta(MySqlDataReader reader)
        {
            return new DetalleVenta
            {
                IdDetalleVenta = reader.GetInt32("IdDetalleVenta"),
                IdVenta = reader.GetInt32("IdVenta"),
                IdProducto = reader.GetInt32("IdProducto"),
                Cantidad = reader.GetInt32("Cantidad"),
                PrecioUnitario = reader.GetDecimal("PrecioUnitario"),
                TotalProducto = reader.GetDecimal("TotalProducto"),
                FechaRegistro = reader.GetDateTime("FechaRegistro"),
                IdEmpleadoResponsable = reader.GetInt32("IdEmpleadoResponsable")
            };
        }
    }
}
