using MySql.Data.MySqlClient;
using ServicioReportes.Dominio.Puertos;

namespace ServicioReportes.Infraestructura;

public sealed class MySqlReporteVentasRepository : IReporteVentasRepository
{
    private readonly string _cadena;
    private readonly ILogger<MySqlReporteVentasRepository> _logger;

    public MySqlReporteVentasRepository(IConfiguration configuration, ILogger<MySqlReporteVentasRepository> logger)
    {
        _cadena = configuration["ConnectionStrings:MySqlConnection"]
            ?? configuration["ConnectionStrings:MySql"]
            ?? configuration["MySql:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__MySqlConnection")
            ?? Environment.GetEnvironmentVariable("MySql__ConnectionString")
            ?? throw new InvalidOperationException("Falta la cadena de conexión MySQL.");
        _logger = logger;
    }

    public async Task<IReadOnlyList<ReporteVentaDetalle>> ObtenerDetalleAsync(
        DateTime desde, DateTime hasta, int? empleadoId, string? estado, CancellationToken cancellationToken = default)
    {
        var resultado = new List<ReporteVentaDetalle>();
        await using var conexion = new MySqlConnection(_cadena);
        await conexion.OpenAsync(cancellationToken);

        var sql = """
            SELECT v.IdVenta, COALESCE(v.CodigoVenta, ''),
                   v.FechaRegistro,
                   CONCAT(TRIM(c.Nombre), ' ', TRIM(c.Apellido)) AS Cliente,
                   CONCAT(TRIM(e.Nombre), ' ', TRIM(e.Apellido)) AS Empleado,
                   v.EstadoVenta, v.MontoTotal
            FROM venta v
            INNER JOIN cliente c ON c.IdCliente = v.IdCliente
            INNER JOIN empleado e ON e.IdEmpleado = v.IdEmpleadoResponsable
            WHERE v.FechaRegistro >= @desde AND v.FechaRegistro < @hasta
            """;

        if (empleadoId.HasValue) sql += " AND v.IdEmpleadoResponsable = @empleadoId";
        if (!string.IsNullOrWhiteSpace(estado)) sql += " AND v.EstadoVenta = @estado";
        sql += " ORDER BY v.FechaRegistro DESC, v.IdVenta DESC;";

        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@desde", desde.Date);
        comando.Parameters.AddWithValue("@hasta", hasta.Date.AddDays(1));
        if (empleadoId.HasValue) comando.Parameters.AddWithValue("@empleadoId", empleadoId.Value);
        if (!string.IsNullOrWhiteSpace(estado)) comando.Parameters.AddWithValue("@estado", estado);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            resultado.Add(new ReporteVentaDetalle(
                lector.GetInt32(0), lector.GetString(1), lector.GetDateTime(2),
                lector.GetString(3), lector.GetString(4), lector.GetString(5),
                lector.GetDecimal(6)));
        }
        return resultado;
    }

    public async Task<IReadOnlyList<ReporteVentaResumen>> ObtenerResumenAsync(
        DateTime desde, DateTime hasta, int? categoriaId, CancellationToken cancellationToken = default)
    {
        var resultado = new List<ReporteVentaResumen>();
        await using var conexion = new MySqlConnection(_cadena);
        await conexion.OpenAsync(cancellationToken);

        var sql = """
            SELECT ca.Nombre, SUM(dv.Cantidad), SUM(dv.TotalProducto)
            FROM detalle_venta dv
            INNER JOIN venta v ON v.IdVenta = dv.IdVenta
            INNER JOIN producto p ON p.IdProducto = dv.IdProducto
            INNER JOIN categoria ca ON ca.IdCategoria = p.IdCategoria
            WHERE v.FechaRegistro >= @desde AND v.FechaRegistro < @hasta
              AND v.EstadoVenta = 'Registrada'
            """;
        if (categoriaId.HasValue) sql += " AND ca.IdCategoria = @categoriaId";
        sql += """
             GROUP BY ca.IdCategoria, ca.Nombre
             ORDER BY SUM(dv.TotalProducto) DESC, ca.Nombre;
            """;

        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@desde", desde.Date);
        comando.Parameters.AddWithValue("@hasta", hasta.Date.AddDays(1));
        if (categoriaId.HasValue) comando.Parameters.AddWithValue("@categoriaId", categoriaId.Value);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            resultado.Add(new ReporteVentaResumen(
                lector.GetString(0), lector.GetInt32(1), lector.GetDecimal(2)));
        }
        return resultado;
    }
}
