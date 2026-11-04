namespace ServicioReportes.Aplicacion.Modelos.Reportes
{
    /// <summary>
    /// Filtros validados del reporte sumariado de productos vendidos.
    /// </summary>
    public sealed record FiltroReporteEstadisticas
    {
        public required int CantidadMinima { get; init; }

        public required string MetodoPago { get; init; }

        public string FiltroTexto =>
            $"Cantidad > {CantidadMinima} · Método de pago: {MetodoPago}";
    }
}
