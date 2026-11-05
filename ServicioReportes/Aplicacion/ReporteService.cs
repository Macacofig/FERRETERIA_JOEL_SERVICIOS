using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using SkiaSharp;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServicioReportes.Dominio.Puertos;

namespace ServicioReportes.Aplicacion;

public sealed class ReporteService
{
    private readonly IReporteVentasRepository _repositorio;

    public ReporteService(IReporteVentasRepository repositorio)
    {
        _repositorio = repositorio;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<IReadOnlyList<ProductoEstable>> ObtenerProductosEstablesAsync(
        ReporteProductoFiltros filtros, CancellationToken cancellationToken = default)
        => _repositorio.ObtenerProductosEstablesAsync(filtros, cancellationToken);

    public Task<IReadOnlyList<VentaProducto>> ObtenerVentasPorProductoAsync(
        ReporteVentaProductoFiltros filtros, CancellationToken cancellationToken = default)
        => _repositorio.ObtenerVentasPorProductoAsync(filtros, cancellationToken);

    public async Task<byte[]> GenerarProductosEstablesPdfAsync(
        ReporteProductoFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var filas = await _repositorio.ObtenerProductosEstablesAsync(filtros, cancellationToken);
        return Document.Create(container => container.Page(page =>
        {
            EncabezadoPdf(page, "Productos de stock estable y precio razonable", usuario,
                $"Precio: {FormatearRango(filtros)} | Cantidad vendida mínima: {filtros.CantidadVendidaMinima?.ToString(CultureInfo.InvariantCulture) ?? "Sin filtro"}");
            page.Content().Table(tabla =>
            {
                tabla.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(12); c.RelativeColumn(18); c.RelativeColumn(12);
                    c.RelativeColumn(18); c.RelativeColumn(12); c.RelativeColumn(12);
                });
                tabla.Header(h =>
                {
                    CeldaEncabezado(h, "Código de producto"); CeldaEncabezado(h, "Nombre");
                    CeldaEncabezado(h, "Costo unitario"); CeldaEncabezado(h, "Descripción");
                    CeldaEncabezado(h, "Marca"); CeldaEncabezado(h, "Categoría");
                });
                foreach (var fila in filas)
                    CeldaFila(tabla, fila.Codigo, fila.Nombre, fila.CostoUnitario.ToString("N2"),
                        fila.Descripcion, fila.Marca, fila.Categoria);
            });
            PiePdf(page, $"Total de productos listados: {filas.Count} | Usuario: {usuario}");
        })).GeneratePdf();
    }

    public async Task<byte[]> GenerarProductosEstablesExcelAsync(
        ReporteProductoFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var filas = await _repositorio.ObtenerProductosEstablesAsync(filtros, cancellationToken);
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Productos estable");
        EncabezadoExcel(hoja, "Productos de stock estable y precio razonable", usuario,
            $"Precio: {FormatearRango(filtros)} | Cantidad vendida mínima: {filtros.CantidadVendidaMinima?.ToString(CultureInfo.InvariantCulture) ?? "Sin filtro"}", 6);
        string[] encabezados = ["Código de producto", "Nombre", "Costo unitario", "Descripción", "Marca", "Categoría"];
        for (int i = 0; i < encabezados.Length; i++) hoja.Cell(5, i + 1).Value = encabezados[i];
        hoja.Range("A5:F5").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        hoja.Range("A5:F5").Style.Font.FontColor = XLColor.White;
        int fila = 6;
        foreach (var producto in filas)
        {
            hoja.Cell(fila, 1).Value = producto.Codigo; hoja.Cell(fila, 2).Value = producto.Nombre;
            hoja.Cell(fila, 3).Value = producto.CostoUnitario; hoja.Cell(fila, 4).Value = producto.Descripcion;
            hoja.Cell(fila, 5).Value = producto.Marca; hoja.Cell(fila, 6).Value = producto.Categoria;
            fila++;
        }
        hoja.Cell(fila, 1).Value = "Total de productos listados:";
        hoja.Cell(fila, 2).Value = filas.Count;
        hoja.Range(fila, 1, fila, 2).Style.Font.Bold = true;
        hoja.Columns().AdjustToContents();
        return GuardarLibro(libro);
    }

    public async Task<byte[]> GenerarVentasProductoPdfAsync(
        ReporteVentaProductoFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var filas = await _repositorio.ObtenerVentasPorProductoAsync(filtros, cancellationToken);
        var total = filas.Sum(fila => fila.TotalProducto);
        return Document.Create(container => container.Page(page =>
        {
            EncabezadoPdf(page, "Ventas por producto", usuario,
                $"Desde: {filtros.Desde:dd/MM/yyyy} | Hasta: {filtros.Hasta:dd/MM/yyyy} | Método de pago: {filtros.MetodoPago}");
            page.Content().Column(contenido =>
            {
                contenido.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c => { c.RelativeColumn(30); c.RelativeColumn(12); c.RelativeColumn(16); });
                    tabla.Header(h => { CeldaEncabezado(h, "Producto"); CeldaEncabezado(h, "Cantidad"); CeldaEncabezado(h, "Total"); });
                    foreach (var fila in filas)
                        CeldaFila(tabla, fila.Nombre, fila.Cantidad.ToString(CultureInfo.InvariantCulture), fila.TotalProducto.ToString("N2"));
                });
                contenido.Item().PaddingTop(12).Text("Gráfico de torta por producto").Bold();
                contenido.Item().Height(220).Svg(GenerarSvgTorta(filas));
            });
            PiePdf(page, $"Total general: {total:N2} Bs. | Usuario: {usuario}");
        })).GeneratePdf();
    }

    public async Task<byte[]> GenerarVentasProductoExcelAsync(
        ReporteVentaProductoFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var filas = await _repositorio.ObtenerVentasPorProductoAsync(filtros, cancellationToken);
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Ventas por producto");
        EncabezadoExcel(hoja, "Ventas por producto", usuario,
            $"Desde: {filtros.Desde:dd/MM/yyyy} | Hasta: {filtros.Hasta:dd/MM/yyyy} | Método de pago: {filtros.MetodoPago}", 4);
        string[] encabezados = ["Nombre de producto", "Cantidad", "Total vendido", "Porcentaje del total"];
        for (int i = 0; i < encabezados.Length; i++) hoja.Cell(5, i + 1).Value = encabezados[i];
        hoja.Range("A5:D5").Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        hoja.Range("A5:D5").Style.Font.FontColor = XLColor.White;
        int fila = 6;
        foreach (var venta in filas)
        {
            hoja.Cell(fila, 1).Value = venta.Nombre;
            hoja.Cell(fila, 2).Value = venta.Cantidad;
            hoja.Cell(fila, 3).Value = venta.TotalProducto;
            fila++;
        }
        hoja.Cell(fila, 1).Value = "Total general:";
        hoja.Cell(fila, 3).FormulaA1 = $"SUM(C6:C{fila - 1})";
        hoja.Range(fila, 1, fila, 3).Style.Font.Bold = true;
        var totalVentas = filas.Sum(filaVenta => filaVenta.TotalProducto);
        for (int filaPorcentaje = 6; filaPorcentaje < fila; filaPorcentaje++)
        {
            var porcentaje = totalVentas == 0 ? 0 : filas[filaPorcentaje - 6].TotalProducto / totalVentas * 100;
            hoja.Cell(filaPorcentaje, 4).Value = porcentaje / 100;
            hoja.Cell(filaPorcentaje, 4).Style.NumberFormat.Format = "0.0%";
        }
        if (filas.Count > 0)
        {
            using var png = GenerarTortaPng(filas);
            hoja.AddPicture(png, "F5").Scale(0.8);
        }
        hoja.Columns().AdjustToContents();
        return GuardarLibro(libro);
    }

    private static void EncabezadoPdf(PageDescriptor page, string titulo, string usuario, string filtros)
    {
        page.Margin(35);
        page.Header().Column(col =>
        {
            col.Item().Text("FERRETERÍA JOEL").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
            col.Item().Text(titulo).FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().Text($"Filtros aplicados: {filtros}");
            col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Usuario: {usuario}");
        });
        page.Footer().Text(text => text.CurrentPageNumber());
    }

    private static void PiePdf(PageDescriptor page, string texto)
    {
        page.Footer().Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);
            col.Item().Text(texto);
        });
    }

    private static void EncabezadoExcel(IXLWorksheet hoja, string titulo, string usuario, string filtros, int columnas)
    {
        hoja.Cell(1, 1).Value = "FERRETERÍA JOEL";
        hoja.Range(1, 1, 1, columnas).Merge();
        hoja.Cell(1, 1).Style.Font.Bold = true; hoja.Cell(1, 1).Style.Font.FontSize = 16;
        hoja.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        hoja.Range(1, 1, 1, columnas).Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        hoja.Cell(2, 1).Value = titulo;
        hoja.Cell(3, 1).Value = $"Filtros aplicados: {filtros} | Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Usuario: {usuario}";
    }

    private static void CeldaEncabezado(TableCellDescriptor header, string texto)
        => header.Cell().Background(Colors.Blue.Medium).Padding(4).Text(texto).FontColor(Colors.White).Bold();
    private static void CeldaFila(TableDescriptor tabla, params string[] valores)
    {
        foreach (var valor in valores) tabla.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(valor);
    }

    private static string FormatearRango(ReporteProductoFiltros filtros)
    {
        var minimo = filtros.PrecioMinimo?.ToString("N2", CultureInfo.InvariantCulture) ?? "Sin mínimo";
        var maximo = filtros.PrecioMaximo?.ToString("N2", CultureInfo.InvariantCulture) ?? "Sin máximo";
        return $"{minimo} - {maximo}";
    }

    private static byte[] GuardarLibro(XLWorkbook libro)
    {
        using var salida = new MemoryStream();
        libro.SaveAs(salida);
        return salida.ToArray();
    }

    private static Stream GenerarTortaPng(IReadOnlyList<VentaProducto> filas)
    {
        var ancho = 520;
        var alto = 300;
        using var bitmap = new SKBitmap(ancho, alto, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var lienzo = new SKCanvas(bitmap);
        lienzo.Clear(SKColors.White);

        var colores = new[]
        {
            new SKColor(68, 114, 196), new SKColor(237, 125, 49),
            new SKColor(112, 173, 71), new SKColor(255, 192, 0),
            new SKColor(91, 155, 213), new SKColor(165, 165, 165)
        };
        var total = filas.Sum(fila => fila.TotalProducto);
        var rectangulo = new SKRect(25, 25, 235, 235);
        float inicio = -90;
        using var pincel = new SKPaint { IsAntialias = true };
        for (var i = 0; i < filas.Count; i++)
        {
            var proporcion = total == 0 ? 0f : (float)filas[i].TotalProducto / (float)total;
            var barrido = proporcion * 360f;
            pincel.Color = colores[i % colores.Length];
            lienzo.DrawArc(rectangulo, inicio, barrido, true, pincel);
            inicio += barrido;

            var leyendaY = 45 + i * 28;
            lienzo.DrawRect(260, leyendaY - 12, 14, 14, pincel);
            var porcentaje = proporcion * 100f;
            pincel.Color = SKColors.Black;
            lienzo.DrawText($"{filas[i].Nombre}: {porcentaje:F1}%", 282, leyendaY, new SKFont(SKTypeface.Default, 14), pincel);
        }

        using var imagen = SKImage.FromBitmap(bitmap);
        using var datos = imagen.Encode(SKEncodedImageFormat.Png, 100);
        var salida = new MemoryStream();
        datos.SaveTo(salida);
        salida.Position = 0;
        return salida;
    }

    private static string GenerarSvgTorta(IReadOnlyList<VentaProducto> filas)
    {
        if (filas.Count == 0) return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"400\" height=\"220\"><text x=\"10\" y=\"20\">Sin datos</text></svg>";
        var total = filas.Sum(fila => fila.TotalProducto);
        if (total == 0) total = 1;
        var colores = new[] { "#4472C4", "#ED7D31", "#70AD47", "#FFC000", "#5B9BD5", "#A5A5A5" };
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"500\" height=\"220\" viewBox=\"0 0 500 220\">");
        double inicio = -Math.PI / 2;
        double cx = 110, cy = 110, radio = 90;
        for (int i = 0; i < filas.Count; i++)
        {
            var angulo = (double)filas[i].TotalProducto / (double)total * Math.PI * 2;
            var fin = inicio + angulo;
            var x1 = cx + radio * Math.Cos(inicio); var y1 = cy + radio * Math.Sin(inicio);
            var x2 = cx + radio * Math.Cos(fin); var y2 = cy + radio * Math.Sin(fin);
            var arco = angulo > Math.PI ? 1 : 0;
            svg.Append($"<path d=\"M {cx:F2} {cy:F2} L {x1:F2} {y1:F2} A {radio} {radio} 0 {arco} 1 {x2:F2} {y2:F2} Z\" fill=\"{colores[i % colores.Length]}\"/>");
            var porcentaje = (double)filas[i].TotalProducto / (double)total * 100;
            var leyendaY = 28 + i * 26;
            svg.Append($"<rect x=\"230\" y=\"{leyendaY - 12}\" width=\"14\" height=\"14\" fill=\"{colores[i % colores.Length]}\"/><text x=\"250\" y=\"{leyendaY}\">{EscapeXml(filas[i].Nombre)}: {porcentaje:F1}%</text>");
            inicio = fin;
        }
        svg.Append("</svg>");
        return svg.ToString();
    }

    private static string EscapeXml(string valor)
        => valor.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
