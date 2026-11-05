namespace ServicioReportes.Aplicacion;

public sealed record ReporteFiltros(
    DateTime? Desde,
    DateTime? Hasta,
    decimal? PrecioMinimo,
    decimal? PrecioMaximo,
    int? CantidadVendidaMinima,
    string? MetodoPago);
