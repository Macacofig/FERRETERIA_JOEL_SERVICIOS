using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServicioReportes.Aplicacion.Modelos.Reportes;

namespace ServicioReportes.Infraestructura.Documentos;

/// <summary>Genera el gráfico de torta reutilizado por PDF y Excel.</summary>
public static class GraficoPieReporte
{
    private static readonly string[] Colores =
    {
        "#EAB308", "#2563EB", "#16A34A", "#DC2626", "#9333EA",
        "#0891B2", "#EA580C", "#4F46E5", "#65A30D", "#DB2777"
    };

    public static string GenerarSvg(ReporteEstadisticas reporte)
    {
        IReadOnlyList<LineaEstadisticaCategoria> filas = reporte.FilasOrdenadas
            .Take(10).ToList();
        decimal total = filas.Sum(fila => fila.TotalRecaudado);
        const double centroX = 190;
        const double centroY = 160;
        const double radio = 115;
        StringBuilder svg = new();
        svg.Append("<svg xmlns='http://www.w3.org/2000/svg' width='760' height='360' viewBox='0 0 760 360'>");
        svg.Append("<rect width='760' height='360' fill='white'/>");
        svg.Append("<text x='20' y='28' font-family='Arial' font-size='18' font-weight='bold' fill='#1d1d1f'>Distribución porcentual de recaudación</text>");

        if (filas.Count == 0 || total <= 0)
        {
            svg.Append("<text x='260' y='185' font-family='Arial' font-size='14' fill='#777'>Sin ventas para los filtros seleccionados.</text></svg>");
            return svg.ToString();
        }

        double inicio = -Math.PI / 2;
        for (int indice = 0; indice < filas.Count; indice++)
        {
            double proporcion = (double)(filas[indice].TotalRecaudado / total);
            double fin = inicio + proporcion * Math.PI * 2;
            double x1 = centroX + radio * Math.Cos(inicio);
            double y1 = centroY + radio * Math.Sin(inicio);
            double x2 = centroX + radio * Math.Cos(fin);
            double y2 = centroY + radio * Math.Sin(fin);
            int arcoGrande = fin - inicio > Math.PI ? 1 : 0;
            string d = $"M {centroX.ToString(CultureInfo.InvariantCulture)} {centroY.ToString(CultureInfo.InvariantCulture)} "
                + $"L {x1.ToString(CultureInfo.InvariantCulture)} {y1.ToString(CultureInfo.InvariantCulture)} "
                + $"A {radio} {radio} 0 {arcoGrande} 1 {x2.ToString(CultureInfo.InvariantCulture)} {y2.ToString(CultureInfo.InvariantCulture)} Z";
            svg.Append($"<path d='{d}' fill='{Colores[indice % Colores.Length]}' stroke='white' stroke-width='2'/>");
            inicio = fin;
        }

        for (int indice = 0; indice < filas.Count; indice++)
        {
            double porcentaje = (double)(filas[indice].TotalRecaudado * 100 / total);
            double y = 70 + indice * 27;
            string producto = Xml(filas[indice].Producto.Length > 27
                ? filas[indice].Producto[..27] + "..."
                : filas[indice].Producto);
            svg.Append($"<rect x='370' y='{y - 12}' width='14' height='14' rx='3' fill='{Colores[indice % Colores.Length]}'/>");
            svg.Append($"<text x='392' y='{y}' font-family='Arial' font-size='12' fill='#444'>{producto} ({porcentaje:N1}%)</text>");
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    public static byte[] GenerarPng(ReporteEstadisticas reporte)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        string svg = GenerarSvg(reporte);
        return Document.Create(documento => documento.Page(pagina =>
            {
                pagina.ContinuousSize(760);
                pagina.Content().Svg(svg);
            }))
            .GenerateImages(new ImageGenerationSettings
            {
                ImageFormat = ImageFormat.Png,
                RasterDpi = 144
            }).First();
    }

    private static string Xml(string texto) => texto
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");
}
