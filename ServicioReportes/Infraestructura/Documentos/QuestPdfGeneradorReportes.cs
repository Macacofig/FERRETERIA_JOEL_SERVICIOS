using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServicioReportes.Aplicacion.Modelos.Reportes;
using ServicioReportes.Aplicacion.Puertos;

namespace ServicioReportes.Infraestructura.Documentos;

/// <summary>Adaptador QuestPDF para los dos reportes oficiales.</summary>
public sealed class QuestPdfGeneradorReportes : IGeneradorReportePdf
{
    static QuestPdfGeneradorReportes() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public byte[] GenerarVentas(ReporteVentas reporte) =>
        Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4.Landscape());
            pagina.Margin(28);
            pagina.Header().Element(x => ReportePdfComun.Cabecera(
                x, "REPORTE DE STOCK", "RPT-STK-01", reporte.GeneradoEl));
            pagina.Content().Column(columna =>
            {
                columna.Spacing(10);
                columna.Item().Element(x => ReportePdfComun.Titulo(
                    x, "Módulo · Reportes", reporte.Titulo,
                    "Productos activos con stock superior al mínimo y precio inferior al máximo."));
                columna.Item().Element(x => ReportePdfComun.Filtros(
                    x, ("Filtros", reporte.Filtro.FiltroTexto),
                    ("Generado", reporte.GeneradoEl.ToString("dd/MM/yyyy HH:mm")),
                    ("Responsable", reporte.GeneradoPor)));
                columna.Item().Element(x => TablaStock(x, reporte));
            });
            pagina.Footer().Element(x => ReportePdfComun.Pie(
                x, reporte.GeneradoPor, reporte.GeneradoEl,
                $"{reporte.TotalRegistros:N0} productos · {reporte.TotalStock:N0} unidades en stock"));
        })).GeneratePdf();

    public byte[] GenerarEstadisticas(ReporteEstadisticas reporte) =>
        Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(28);
            pagina.Header().Element(x => ReportePdfComun.Cabecera(
                x, "INFORME DE VENTAS", "RPT-VTA-02", reporte.GeneradoEl));
            pagina.Content().Column(columna =>
            {
                columna.Spacing(10);
                columna.Item().Element(x => ReportePdfComun.Titulo(
                    x, "Módulo · Reportes", reporte.Titulo,
                    "Información sumariada por producto con distribución porcentual de recaudación."));
                columna.Item().Element(x => ReportePdfComun.Filtros(
                    x, ("Filtros", reporte.Filtro.FiltroTexto),
                    ("Generado", reporte.GeneradoEl.ToString("dd/MM/yyyy HH:mm")),
                    ("Responsable", reporte.GeneradoPor)));
                columna.Item().Height(235).Svg(GraficoPieReporte.GenerarSvg(reporte));
                columna.Item().Element(x => TablaVentas(x, reporte));
            });
            pagina.Footer().Element(x => ReportePdfComun.Pie(
                x, reporte.GeneradoPor, reporte.GeneradoEl,
                $"{reporte.TotalProductos:N0} productos · Bs {reporte.TotalImporte:N2} recaudados"));
        })).GeneratePdf();

    private static void TablaStock(IContainer contenedor, ReporteVentas reporte)
    {
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.2f); c.RelativeColumn(2.1f); c.RelativeColumn(1.1f);
                c.RelativeColumn(2.4f); c.RelativeColumn(1.5f); c.RelativeColumn(1.5f);
                c.RelativeColumn(1f);
            });
            tabla.Header(h => { foreach (string titulo in new[] { "Código", "Producto", "Precio", "Descripción", "Marca", "Categoría", "Stock" })
                    h.Cell().Element(ReportePdfComun.EstiloCabecera).Text(titulo).Bold(); });
            for (int i = 0; i < reporte.Lineas.Count; i++)
            {
                LineaReporteVentas fila = reporte.Lineas[i];
                bool alterna = i % 2 == 1;
                foreach (string valor in new[] { fila.Codigo, fila.Producto, $"Bs {fila.Precio:N2}", fila.Descripcion, fila.Marca, fila.Categoria, fila.CantidadStock.ToString("N0") })
                    tabla.Cell().Element(x => ReportePdfComun.EstiloCelda(x, alterna)).Text(valor);
            }
        });
    }

    private static void TablaVentas(IContainer contenedor, ReporteEstadisticas reporte)
    {
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1.2f); c.RelativeColumn(1.7f); c.RelativeColumn(1.2f); });
            tabla.Header(h => { foreach (string titulo in new[] { "Producto", "Cantidad vendida", "Total recaudado", "%" })
                    h.Cell().Element(ReportePdfComun.EstiloCabecera).Text(titulo).Bold(); });
            for (int i = 0; i < reporte.FilasOrdenadas.Count; i++)
            {
                LineaEstadisticaCategoria fila = reporte.FilasOrdenadas[i];
                bool alterna = i % 2 == 1;
                foreach (string valor in new[] { fila.Producto, fila.CantidadTotal.ToString("N0"), $"Bs {fila.TotalRecaudado:N2}", $"{fila.Participacion:N2}%" })
                    tabla.Cell().Element(x => ReportePdfComun.EstiloCelda(x, alterna)).Text(valor);
            }
        });
    }
}
