using ServicioReportes.Aplicacion.Modelos.Reportes;

namespace ServicioReportes.Aplicacion.Puertos
{
    /// <summary>Puerto de salida para la exportación de reportes a PDF.</summary>
    public interface IGeneradorReportePdf
    {
        byte[] GenerarVentas(ReporteVentas reporte);

        byte[] GenerarEstadisticas(ReporteEstadisticas reporte);
    }
}
