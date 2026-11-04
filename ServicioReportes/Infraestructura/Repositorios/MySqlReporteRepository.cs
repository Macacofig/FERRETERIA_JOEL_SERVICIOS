using MySql.Data.MySqlClient;
using ServicioReportes.Aplicacion.Modelos.Reportes;
using ServicioReportes.Aplicacion.Puertos;
using ServicioReportes.Infraestructura.Conexion;

namespace ServicioReportes.Infraestructura.Repositorios;

/// <summary>Adaptador MySQL/ADO.NET para los dos reportes oficiales.</summary>
public sealed class MySqlReporteRepository : IReporteRepository
{
    private readonly IDbConnectionFactory _fabricaConexion;

    public MySqlReporteRepository(IDbConnectionFactory fabricaConexion)
    {
        _fabricaConexion = fabricaConexion;
    }

    public async Task<IReadOnlyList<LineaReporteVentas>> ObtenerVentasAsync(
        FiltroReporteVentas filtro,
        CancellationToken cancelacion = default)
    {
        const string sql = @"
            SELECT p.Codigo, p.Nombre AS Producto,
                   p.PrecioVenta AS Precio, p.Descripcion,
                   m.Nombre AS Marca, c.Nombre AS Categoria,
                   p.CantidadStock
            FROM producto p
            INNER JOIN marca m ON p.Marca = m.IdMarca
            INNER JOIN categoria c ON p.IdCategoria = c.IdCategoria
            WHERE p.CantidadStock > @stockMinimo
              AND p.PrecioVenta < @precioMaximo
              AND p.Estado = 1
            ORDER BY p.Codigo ASC";

        List<LineaReporteVentas> resultado = new();
        await using MySqlConnection conexion = _fabricaConexion.CreateConnection();
        await conexion.OpenAsync(cancelacion);
        await using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.AddWithValue("@stockMinimo", filtro.StockMinimo);
        comando.Parameters.AddWithValue("@precioMaximo", filtro.PrecioMaximo);
        await using MySqlDataReader lector =
            (MySqlDataReader)await comando.ExecuteReaderAsync(cancelacion);

        while (await lector.ReadAsync(cancelacion))
        {
            resultado.Add(new LineaReporteVentas
            {
                Codigo = Texto(lector, "Codigo") ?? string.Empty,
                Producto = Texto(lector, "Producto") ?? "Sin nombre",
                Precio = lector.GetDecimal("Precio"),
                Descripcion = Texto(lector, "Descripcion") ?? string.Empty,
                Marca = Texto(lector, "Marca") ?? "Sin marca",
                Categoria = Texto(lector, "Categoria") ?? "Sin categoría",
                CantidadStock = lector.GetInt32("CantidadStock")
            });
        }

        return resultado;
    }

    public async Task<IReadOnlyList<LineaEstadisticaCategoria>> ObtenerEstadisticasAsync(
        FiltroReporteEstadisticas filtro,
        CancellationToken cancelacion = default)
    {
        const string sql = @"
            SELECT p.Nombre AS Producto,
                   SUM(dv.Cantidad) AS CantidadTotal,
                   SUM(dv.TotalProducto) AS TotalRecaudado
            FROM detalle_venta dv
            INNER JOIN producto p ON dv.IdProducto = p.IdProducto
            INNER JOIN venta v ON dv.IdVenta = v.IdVenta
            WHERE dv.Cantidad > @cantidadMinima
              AND v.MetodoPago = @metodoPago
              AND v.EstadoVenta = 'Registrada'
            GROUP BY p.IdProducto, p.Nombre
            ORDER BY TotalRecaudado DESC";

        List<LineaEstadisticaCategoria> resultado = new();
        await using MySqlConnection conexion = _fabricaConexion.CreateConnection();
        await conexion.OpenAsync(cancelacion);
        await using MySqlCommand comando = new(sql, conexion);
        comando.Parameters.AddWithValue("@cantidadMinima", filtro.CantidadMinima);
        comando.Parameters.AddWithValue("@metodoPago", filtro.MetodoPago);
        await using MySqlDataReader lector =
            (MySqlDataReader)await comando.ExecuteReaderAsync(cancelacion);

        while (await lector.ReadAsync(cancelacion))
        {
            resultado.Add(new LineaEstadisticaCategoria
            {
                Producto = Texto(lector, "Producto") ?? "Producto sin nombre",
                CantidadTotal = Convert.ToInt32(lector["CantidadTotal"]),
                TotalRecaudado = Convert.ToDecimal(lector["TotalRecaudado"])
            });
        }

        return resultado;
    }

    public Task<IReadOnlyList<CategoriaOpcion>> ObtenerCategoriasAsync(
        CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<CategoriaOpcion>>(Array.Empty<CategoriaOpcion>());

    public Task<string?> ObtenerNombreCategoriaAsync(
        int idCategoria,
        CancellationToken cancelacion = default) =>
        Task.FromResult<string?>(null);

    private static string? Texto(MySqlDataReader lector, string columna)
    {
        object valor = lector[columna];
        return valor is DBNull or null ? null : valor.ToString();
    }
}
