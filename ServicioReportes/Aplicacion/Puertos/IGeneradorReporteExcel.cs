using ServicioReportes.Aplicacion.Modelos.Reportes;

namespace ServicioReportes.Aplicacion.Puertos
{
    /// <summary>Puerto de salida para la exportación de reportes a Excel.</summary>
    public interface IGeneradorReporteExcel
    {
        byte[] GenerarVentas(ReporteVentas reporte);

        byte[] GenerarEstadisticas(ReporteEstadisticas reporte);
    }
}
