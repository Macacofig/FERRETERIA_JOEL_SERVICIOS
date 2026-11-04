using ClosedXML.Excel;
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

    public async Task<byte[]> GenerarDetallePdfAsync(ReporteFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var filas = await _repositorio.ObtenerDetalleAsync(
            filtros.Desde, filtros.Hasta, filtros.EmpleadoId, filtros.Estado, cancellationToken);
        var generado = DateTime.Now;
        var total = filas.Sum(fila => fila.MontoTotal);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                Encabezado(page, "Reporte detallado de ventas", generado, usuario);
                page.Content().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(10); c.RelativeColumn(11); c.RelativeColumn(13);
                        c.RelativeColumn(13); c.RelativeColumn(10); c.RelativeColumn(10);
                    });
                    tabla.Header(header =>
                    {
                        HeaderCelda(header, "Código"); HeaderCelda(header, "Fecha"); HeaderCelda(header, "Cliente");
                        HeaderCelda(header, "Empleado"); HeaderCelda(header, "Estado"); HeaderCelda(header, "Total");
                    });
                    foreach (var fila in filas)
                    {
                        FilaCelda(tabla, fila.Codigo, fila.Fecha.ToString("dd/MM/yyyy HH:mm"), fila.Cliente,
                            fila.Empleado, fila.Estado, fila.MontoTotal.ToString("C2"));
                    }
                });
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);
                    col.Item().Text($"Total de ventas: {total:C2} | Registros: {filas.Count} | Usuario: {usuario}");
                });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerarDetalleExcelAsync(ReporteFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var filas = await _repositorio.ObtenerDetalleAsync(
            filtros.Desde, filtros.Hasta, filtros.EmpleadoId, filtros.Estado, cancellationToken);
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Ventas detalladas");
        EncabezadoExcel(hoja, "Reporte detallado de ventas", usuario);
        string[] encabezados = ["Código", "Fecha", "Cliente", "Empleado", "Estado", "Total"];
        for (var i = 0; i < encabezados.Length; i++)
            hoja.Cell(4, i + 1).Value = encabezados[i];
        hoja.Range("A4:F4").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        hoja.Range("A4:F4").Style.Font.FontColor = XLColor.White;

        var filaExcel = 5;
        foreach (var fila in filas)
        {
            hoja.Cell(filaExcel, 1).Value = fila.Codigo;
            hoja.Cell(filaExcel, 2).Value = fila.Fecha;
            hoja.Cell(filaExcel, 3).Value = fila.Cliente;
            hoja.Cell(filaExcel, 4).Value = fila.Empleado;
            hoja.Cell(filaExcel, 5).Value = fila.Estado;
            hoja.Cell(filaExcel, 6).Value = fila.MontoTotal;
            filaExcel++;
        }
        hoja.Cell(filaExcel, 5).Value = "Total:";
        hoja.Cell(filaExcel, 6).FormulaA1 = $"SUM(F5:F{filaExcel - 1})";
        hoja.Range(filaExcel, 5, filaExcel, 6).Style.Font.Bold = true;
        hoja.Columns().AdjustToContents();
        using var salida = new MemoryStream();
        libro.SaveAs(salida);
        return salida.ToArray();
    }

    public async Task<byte[]> GenerarResumenPdfAsync(ReporteFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var filas = await _repositorio.ObtenerResumenAsync(
            filtros.Desde, filtros.Hasta, filtros.CategoriaId, cancellationToken);
        var generado = DateTime.Now;
        var cantidad = filas.Sum(fila => fila.CantidadVendida);
        var total = filas.Sum(fila => fila.TotalVentas);
        var maximo = filas.Count == 0 ? 1m : filas.Max(fila => fila.TotalVentas);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                Encabezado(page, "Resumen de ventas por categoría", generado, usuario);
                page.Content().Column(contenido =>
                {
                    contenido.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c => { c.RelativeColumn(16); c.RelativeColumn(8); c.RelativeColumn(10); });
                        tabla.Header(header => { HeaderCelda(header, "Categoría"); HeaderCelda(header, "Cantidad"); HeaderCelda(header, "Ventas"); });
                        foreach (var fila in filas)
                        {
                            FilaCelda(tabla, fila.Categoria, fila.CantidadVendida.ToString(), fila.TotalVentas.ToString("C2"));
                        }
                    });
                    contenido.Item().PaddingTop(12).Text("Gráfico de ventas por categoría").Bold();
                    foreach (var fila in filas)
                    {
                        var proporcion = maximo == 0 ? 0 : (double)(fila.TotalVentas / maximo);
                        contenido.Item().Text($"{fila.Categoria}: {fila.TotalVentas:C2}");
                        contenido.Item().Height(8).Background(Colors.Blue.Lighten2)
                            .Width((float)(proporcion * 100));
                    }
                });
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);
                    col.Item().Text($"Cantidad vendida: {cantidad} | Total: {total:C2} | Usuario: {usuario}");
                });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerarResumenExcelAsync(ReporteFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var filas = await _repositorio.ObtenerResumenAsync(
            filtros.Desde, filtros.Hasta, filtros.CategoriaId, cancellationToken);
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Resumen por categoría");
        EncabezadoExcel(hoja, "Resumen de ventas por categoría", usuario);
        string[] encabezados = ["Categoría", "Cantidad vendida", "Total vendido"];
        for (var i = 0; i < encabezados.Length; i++)
            hoja.Cell(4, i + 1).Value = encabezados[i];
        hoja.Range("A4:C4").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        hoja.Range("A4:C4").Style.Font.FontColor = XLColor.White;

        var filaExcel = 5;
        foreach (var fila in filas)
        {
            hoja.Cell(filaExcel, 1).Value = fila.Categoria;
            hoja.Cell(filaExcel, 2).Value = fila.CantidadVendida;
            hoja.Cell(filaExcel, 3).Value = fila.TotalVentas;
            filaExcel++;
        }
        hoja.Cell(filaExcel, 1).Value = "Totales:";
        hoja.Cell(filaExcel, 2).FormulaA1 = $"SUM(B5:B{filaExcel - 1})";
        hoja.Cell(filaExcel, 3).FormulaA1 = $"SUM(C5:C{filaExcel - 1})";
        hoja.Range(filaExcel, 1, filaExcel, 3).Style.Font.Bold = true;

        if (filas.Count > 0)
        {
            hoja.Cell(4, 4).Value = "Gráfico: ventas por categoría";
            hoja.Range("D4:D4").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
            hoja.Range("D4:D4").Style.Font.FontColor = XLColor.White;
            decimal maximo = filas.Max(fila => fila.TotalVentas);
            for (var i = 0; i < filas.Count; i++)
            {
                int bloques = maximo == 0 ? 0 : (int)Math.Round(filas[i].TotalVentas / maximo * 20, MidpointRounding.AwayFromZero);
                hoja.Cell(5 + i, 4).Value = new string('█', bloques);
                hoja.Cell(5 + i, 4).Style.Font.FontColor = XLColor.FromHtml("#4472C4");
            }
        }
        hoja.Columns().AdjustToContents();
        using var salida = new MemoryStream();
        libro.SaveAs(salida);
        return salida.ToArray();
    }

    private static void Encabezado(PageDescriptor page, string titulo, DateTime generado, string usuario)
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(25);
        page.Header().Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("FERRETERÍA JOEL").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                row.RelativeItem().AlignRight().Text(generado.ToString("dd/MM/yyyy HH:mm:ss"));
            });
            col.Item().Text(titulo).FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().Text($"Generado por: {usuario}").FontSize(9);
        });
    }

    private static void HeaderCelda(TableCellDescriptor header, string texto)
        => header.Cell().Background(Colors.Blue.Medium).Padding(4).Text(texto).FontColor(Colors.White).Bold();

    private static void FilaCelda(TableDescriptor tabla, params string[] valores)
    {
        foreach (var valor in valores)
            tabla.Cell().BorderBottom(0.5f).Padding(4).Text(valor);
    }

    private static void EncabezadoExcel(IXLWorksheet hoja, string titulo, string usuario)
    {
        hoja.Cell(1, 1).Value = "FERRETERÍA JOEL";
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#FFFFFF");
        hoja.Range(1, 1, 1, 6).Merge();
        hoja.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        hoja.Cell(2, 1).Value = titulo;
        hoja.Cell(2, 1).Style.Font.Bold = true;
        hoja.Cell(3, 1).Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Usuario: {usuario}";
        hoja.Range(3, 1, 3, 6).Merge();
    }
}
