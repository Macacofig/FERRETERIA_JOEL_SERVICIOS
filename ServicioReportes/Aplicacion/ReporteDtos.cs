namespace ServicioReportes.Aplicacion;

public sealed record ReporteFiltros(DateTime Desde, DateTime Hasta, int? EmpleadoId, string? Estado, int? CategoriaId);
