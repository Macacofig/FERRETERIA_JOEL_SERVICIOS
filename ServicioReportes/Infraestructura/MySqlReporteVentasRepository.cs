using MySql.Data.MySqlClient;
using ServicioReportes.Dominio.Puertos;

namespace ServicioReportes.Infraestructura;

public sealed class MySqlReporteVentasRepository : IReporteVentasRepository
{
    private readonly string _cadena;

    public MySqlReporteVentasRepository(IConfiguration configuration)
    {
        _cadena = configuration["ConnectionStrings:MySqlConnection"]
            ?? configuration["ConnectionStrings:MySql"]
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__MySqlConnection")
            ?? throw new InvalidOperationException("Falta la cadena de conexión MySQL.");
    }

    public async Task<IReadOnlyList<ProductoEstable>> ObtenerProductosEstablesAsync(
        ReporteProductoFiltros filtros, CancellationToken cancellationToken = default)
    {
        var resultado = new List<ProductoEstable>();
        await using var conexion = new MySqlConnection(_cadena);
        await conexion.OpenAsync(cancellationToken);
        var sql = """
            SELECT p.Codigo, p.Nombre, p.PrecioVenta, COALESCE(p.Descripcion, ''),
                   COALESCE(m.Nombre, ''), ca.Nombre
            FROM producto p
            INNER JOIN marca m ON m.IdMarca = p.Marca
            INNER JOIN categoria ca ON ca.IdCategoria = p.IdCategoria
            WHERE p.CantidadStock > 10 AND p.PrecioVenta < 500
            """;
        if (filtros.PrecioMinimo.HasValue) sql += " AND p.PrecioVenta >= @precioMinimo";
        if (filtros.PrecioMaximo.HasValue) sql += " AND p.PrecioVenta <= @precioMaximo";
        if (filtros.CantidadVendidaMinima.HasValue)
            sql += """
                AND EXISTS (
                    SELECT 1 FROM detalle_venta dv
                    WHERE dv.IdProducto = p.IdProducto
                    GROUP BY dv.IdProducto
                    HAVING SUM(dv.Cantidad) >= @cantidadVendidaMinima
                )
                """;
        sql += " ORDER BY p.Codigo ASC;";

        await using var comando = new MySqlCommand(sql, conexion);
        if (filtros.PrecioMinimo.HasValue) comando.Parameters.AddWithValue("@precioMinimo", filtros.PrecioMinimo.Value);
        if (filtros.PrecioMaximo.HasValue) comando.Parameters.AddWithValue("@precioMaximo", filtros.PrecioMaximo.Value);
        if (filtros.CantidadVendidaMinima.HasValue) comando.Parameters.AddWithValue("@cantidadVendidaMinima", filtros.CantidadVendidaMinima.Value);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
            resultado.Add(new ProductoEstable(lector.GetString(0), lector.GetString(1), lector.GetDecimal(2),
                lector.GetString(3), lector.GetString(4), lector.GetString(5)));
        return resultado;
    }

    public async Task<IReadOnlyList<VentaProducto>> ObtenerVentasPorProductoAsync(
        ReporteVentaProductoFiltros filtros, CancellationToken cancellationToken = default)
    {
        var resultado = new List<VentaProducto>();
        await using var conexion = new MySqlConnection(_cadena);
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new MySqlCommand("""
            SELECT p.Nombre, SUM(dv.Cantidad), SUM(dv.TotalProducto)
            FROM producto p
            INNER JOIN detalle_venta dv ON dv.IdProducto = p.IdProducto
            INNER JOIN venta v ON v.IdVenta = dv.IdVenta
            WHERE dv.Cantidad > 3
              AND v.FechaRegistro >= @desde AND v.FechaRegistro < @hasta
              AND v.MetodoPago = @metodoPago AND v.EstadoVenta = 'Registrada'
            GROUP BY p.IdProducto, p.Nombre
            ORDER BY SUM(dv.TotalProducto) DESC, p.Nombre ASC;
            """, conexion);
        comando.Parameters.AddWithValue("@desde", filtros.Desde.Date);
        comando.Parameters.AddWithValue("@hasta", filtros.Hasta.Date.AddDays(1));
        comando.Parameters.AddWithValue("@metodoPago", filtros.MetodoPago);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
            resultado.Add(new VentaProducto(lector.GetString(0), lector.GetInt32(1), lector.GetDecimal(2)));
        return resultado;
    }
}
