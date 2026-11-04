using ServicioReportes.Aplicacion.Modelos.Reportes;
using ServicioReportes.Aplicacion.Puertos;
using ServicioReportes.Dominio.Utilidades;

namespace ServicioReportes.Aplicacion.Servicios;

/// <summary>Orquesta consultas y exportaciones sin conocer ADO.NET ni documentos.</summary>
public sealed class FachadaReportes
{
    private readonly IReporteRepository _repositorio;
    private readonly IGeneradorReportePdf _generadorPdf;
    private readonly IGeneradorReporteExcel _generadorExcel;

    public FachadaReportes(IReporteRepository repositorio,
        IGeneradorReportePdf generadorPdf,
        IGeneradorReporteExcel generadorExcel)
    {
        _repositorio = repositorio;
        _generadorPdf = generadorPdf;
        _generadorExcel = generadorExcel;
    }

    public async Task<ReporteVentas> ObtenerVentasAsync(
        SolicitudReporteVentas solicitud, string? usuario,
        CancellationToken cancelacion = default)
    {
        FiltroReporteVentas filtro = solicitud.CrearFiltro();
        IReadOnlyList<LineaReporteVentas> lineas =
            await _repositorio.ObtenerVentasAsync(filtro, cancelacion);
        string generadoPor = NormalizarUsuario(usuario);

        return new ReporteVentas
        {
            Filtro = filtro,
            Lineas = lineas,
            GeneradoPorCompleto = generadoPor,
            GeneradoPor = NombreAbreviado.De(generadoPor),
            GeneradoEl = DateTime.Now
        };
    }

    public async Task<ReporteEstadisticas> ObtenerEstadisticasAsync(
        SolicitudReporteEstadisticas solicitud, string? usuario,
        CancellationToken cancelacion = default)
    {
        FiltroReporteEstadisticas filtro = solicitud.CrearFiltro();
        IReadOnlyList<LineaEstadisticaCategoria> filas =
            await _repositorio.ObtenerEstadisticasAsync(filtro, cancelacion);
        decimal total = filas.Sum(fila => fila.TotalRecaudado);
        IReadOnlyList<LineaEstadisticaCategoria> conParticipacion = filas
            .Select(fila => fila with
            {
                Participacion = total == 0
                    ? 0
                    : decimal.Round(fila.TotalRecaudado * 100 / total, 2)
            }).ToList();
        string generadoPor = NormalizarUsuario(usuario);

        return new ReporteEstadisticas
        {
            Filtro = filtro,
            Filas = conParticipacion,
            GeneradoPorCompleto = generadoPor,
            GeneradoPor = NombreAbreviado.De(generadoPor),
            GeneradoEl = DateTime.Now
        };
    }

    public Task<IReadOnlyList<CategoriaOpcion>> ObtenerCategoriasAsync(
        CancellationToken cancelacion = default) =>
        _repositorio.ObtenerCategoriasAsync(cancelacion);

    public async Task<ArchivoReporte> ExportarVentasPdfAsync(
        SolicitudReporteVentas solicitud, string? usuario,
        CancellationToken cancelacion = default)
    {
        ReporteVentas reporte = await ObtenerVentasAsync(solicitud, usuario, cancelacion);
        return new ArchivoReporte(_generadorPdf.GenerarVentas(reporte),
            NombreArchivo("Reporte-Stock", "pdf", reporte.GeneradoEl), ArchivoReporte.TipoPdf);
    }

    public async Task<ArchivoReporte> ExportarVentasExcelAsync(
        SolicitudReporteVentas solicitud, string? usuario,
        CancellationToken cancelacion = default)
    {
        ReporteVentas reporte = await ObtenerVentasAsync(solicitud, usuario, cancelacion);
        return new ArchivoReporte(_generadorExcel.GenerarVentas(reporte),
            NombreArchivo("Reporte-Stock", "xlsx", reporte.GeneradoEl), ArchivoReporte.TipoExcel);
    }

    public async Task<ArchivoReporte> ExportarEstadisticasPdfAsync(
        SolicitudReporteEstadisticas solicitud, string? usuario,
        CancellationToken cancelacion = default)
    {
        ReporteEstadisticas reporte = await ObtenerEstadisticasAsync(solicitud, usuario, cancelacion);
        return new ArchivoReporte(_generadorPdf.GenerarEstadisticas(reporte),
            NombreArchivo("Reporte-Ventas-Producto", "pdf", reporte.GeneradoEl), ArchivoReporte.TipoPdf);
    }

    public async Task<ArchivoReporte> ExportarEstadisticasExcelAsync(
        SolicitudReporteEstadisticas solicitud, string? usuario,
        CancellationToken cancelacion = default)
    {
        ReporteEstadisticas reporte = await ObtenerEstadisticasAsync(solicitud, usuario, cancelacion);
        return new ArchivoReporte(_generadorExcel.GenerarEstadisticas(reporte),
            NombreArchivo("Reporte-Ventas-Producto", "xlsx", reporte.GeneradoEl), ArchivoReporte.TipoExcel);
    }

    private static string NormalizarUsuario(string? usuario) =>
        string.IsNullOrWhiteSpace(usuario) ? "Usuario del sistema" : usuario.Trim();

    private static string NombreArchivo(string prefijo, string extension, DateTime fecha) =>
        $"{prefijo}-{fecha:yyyyMMdd-HHmmss}.{extension}";
}
