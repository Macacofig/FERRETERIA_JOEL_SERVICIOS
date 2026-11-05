using System.Globalization;
using System.Text;
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

    // ---------- MÉTODOS DE CONSULTA BASE ----------

    public Task<IReadOnlyList<ProductoEstable>> ObtenerProductosEstablesAsync(
        ReporteProductoFiltros filtros, CancellationToken cancellationToken = default)
        => _repositorio.ObtenerProductosEstablesAsync(filtros, cancellationToken);

    public Task<IReadOnlyList<VentaProducto>> ObtenerVentasPorProductoAsync(
        ReporteVentaProductoFiltros filtros, CancellationToken cancellationToken = default)
        => _repositorio.ObtenerVentasPorProductoAsync(filtros, cancellationToken);

    // ---------- MÉTODOS DE EXPORTACIÓN PDF (DISEÑO CORPORATIVO ANTIGUO) ----------

    public async Task<byte[]> GenerarProductosEstablesPdfAsync(
        ReporteProductoFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var filas = await _repositorio.ObtenerProductosEstablesAsync(filtros, cancellationToken);

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.Header().Element(x => Cabecera(x, "REPORTE DE STOCK", "RPT-STK-01", DateTime.Now));

            page.Content().Column(col =>
            {
                col.Spacing(15);
                col.Item().Element(x => Titulo(x, "MÓDULO REPORTES", "Productos de stock estable y precio razonable",
                    "Productos activos con stock superior al mínimo y precio inferior al máximo."));

                col.Item().Element(x => Filtros(x,
                    ("FILTROS", $"Stock > {filtros.StockMinimo ?? 10} · Precio < Bs {(filtros.PrecioMaximo ?? 500m):N2}"),
                    ("GENERADO", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
                    ("RESPONSABLE", usuario)));

                col.Item().Element(x => TablaStock(x, filas));
            });

            page.Footer().Element(x => Pie(x, usuario, DateTime.Now, $"{filas.Count} productos · {filas.Sum(f => f.CantidadStock)} unidades en stock"));
        })).GeneratePdf();
    }

    public async Task<byte[]> GenerarVentasProductoPdfAsync(
        ReporteVentaProductoFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var ventas = await _repositorio.ObtenerVentasPorProductoAsync(filtros, cancellationToken);
        var totalImporte = ventas.Sum(v => v.TotalProducto);
        var segmentos = PrepararSegmentos(ventas);

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.Header().Element(x => Cabecera(x, "INFORME ESTADÍSTICO", "RPT-VTA-02", DateTime.Now));

            page.Content().Column(col =>
            {
                col.Spacing(15);
                col.Item().Element(x => Titulo(x, "MÓDULO REPORTES", "Ventas por producto y medio de cobro",
                    "Información sumariada por producto con distribución porcentual de recaudación."));

                col.Item().Element(x => Filtros(x,
                    ("FILTROS", $"Cantidad > {filtros.CantidadMinima ?? 3} · Método de pago: {filtros.MetodoPago}"),
                    ("GENERADO", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
                    ("RESPONSABLE", usuario)));

                // La leyenda va como texto nativo de QuestPDF: el texto dentro de
                // un SVG no se rasteriza en todas las plataformas.
                if (segmentos.Count == 0)
                {
                    col.Item().Text("Sin ventas para los filtros seleccionados.")
                        .FontSize(9).FontColor("#5F5F66");
                }
                else
                {
                    col.Item().Row(row =>
                    {
                        // Height + AlignCenter: el SVG conserva su proporción sin
                        // entrar en conflicto con el ancho disponible de la página.
                        row.RelativeItem(1.05f).Height(230).AlignCenter()
                            .Svg(GenerarSvgTorta(segmentos));
                        row.RelativeItem(0.95f).PaddingLeft(12)
                            .Element(x => LeyendaTorta(x, segmentos));
                    });
                }

                col.Item().Element(x => TablaVentas(x, ventas));
            });

            page.Footer().Element(x => Pie(x, usuario, DateTime.Now, $"{ventas.Count} productos · Bs {totalImporte:N2} recaudados"));
        })).GeneratePdf();
    }

    // ---------- COMPONENTES VISUALES PDF (ESTILO ANTIGUO) ----------

    private static void Cabecera(IContainer x, string tipo, string codigo, DateTime fecha) =>
        x.PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Row(r =>
            {
                r.AutoItem().Width(45).Height(45).Background("#EAB308").AlignCenter().AlignMiddle()
                 .Text("FJ").Bold().FontSize(22).FontColor("#1D1D1F");
                r.RelativeItem().PaddingLeft(10).Column(c =>
                {
                    c.Item().Text("FERRETERÍA JOEL").Bold().FontSize(12).FontColor("#1D1D1F");
                    c.Item().Text("Herramientas y materiales de construcción").FontSize(8).FontColor("#5F5F66");
                    c.Item().Text("Av. Central N. 123, El Alto · La Paz | +591 2 123 4567 | contacto@ferreteriajoel.com")
                     .FontSize(7).FontColor("#5F5F66");
                });
            });
            row.AutoItem().AlignRight().Column(c =>
            {
                c.Item().Text(tipo).Bold().FontSize(11).FontColor("#1D1D1F").AlignRight();
                c.Item().Text(codigo).FontSize(9).FontColor("#5F5F66").AlignRight();
                c.Item().Text($"Emitido: {fecha:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#5F5F66").AlignRight();
            });
        });

    private static void Titulo(IContainer x, string modulo, string titulo, string descripcion) =>
        x.Column(col =>
        {
            col.Item().Text(modulo).FontSize(8).FontColor("#EAB308").Bold();
            col.Item().Text(titulo).FontSize(16).Bold().FontColor("#1D1D1F");
            col.Item().Text(descripcion).FontSize(9).FontColor("#5F5F66");
        });

    private static void Filtros(IContainer x, params (string etiqueta, string valor)[] campos) =>
        x.Background("#F8FAFC").Border(1).BorderColor("#E7E7EA").Padding(10).Row(row =>
        {
            foreach (var (etiqueta, valor) in campos)
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(etiqueta).FontSize(7).Bold().FontColor("#8A8A91");
                    col.Item().Text(valor).FontSize(9).FontColor("#1D1D1F").Bold();
                });
            }
        });

    private static void Pie(IContainer x, string responsable, DateTime fecha, string total) =>
        x.BorderTop(1).BorderColor("#D2D2D7").PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text($"Generado el {fecha:dd/MM/yyyy HH:mm:ss} - Responsable: {responsable}").FontSize(8).FontColor("#5F5F66");
            row.RelativeItem().AlignCenter().Text(total).FontSize(8).Bold().FontColor("#1D1D1F");
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Página ").FontSize(8).FontColor("#5F5F66");
                t.CurrentPageNumber().FontSize(8).FontColor("#5F5F66");
                t.Span(" de ").FontSize(8).FontColor("#5F5F66");
                t.TotalPages().FontSize(8).FontColor("#5F5F66");
            });
        });

    private static void TablaStock(IContainer contenedor, IReadOnlyList<ProductoEstable> filas) =>
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.2f); c.RelativeColumn(2.2f); c.RelativeColumn(1.1f);
                c.RelativeColumn(2.6f); c.RelativeColumn(1.5f); c.RelativeColumn(1.5f);
            });
            tabla.Header(h => {
                foreach (string titulo in new[] { "Código", "Nombre", "Precio", "Descripción", "Marca", "Categoría" })
                    h.Cell().Element(EstiloCabecera).Text(titulo).Bold();
            });
            for (int i = 0; i < filas.Count; i++)
            {
                var f = filas[i];
                bool alterna = i % 2 == 1;
                foreach (string valor in new[] { f.Codigo, f.Nombre, $"Bs {f.Precio:N2}", f.Descripcion, f.Marca, f.Categoria })
                    tabla.Cell().Element(x => EstiloCelda(x, alterna)).Text(valor).FontSize(8).FontColor("#1D1D1F");
            }
        });

    private static void TablaVentas(IContainer contenedor, IReadOnlyList<VentaProducto> filas) =>
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1.4f); c.RelativeColumn(1.7f); });
            tabla.Header(h => {
                foreach (string titulo in new[] { "Nombre", "CantidadTotal", "TotalRecaudado" })
                    h.Cell().Element(EstiloCabecera).Text(titulo).Bold();
            });
            for (int i = 0; i < filas.Count; i++)
            {
                var f = filas[i];
                bool alterna = i % 2 == 1;
                foreach (string valor in new[] { f.Nombre, f.Cantidad.ToString("N0"), $"Bs {f.TotalProducto:N2}" })
                    tabla.Cell().Element(x => EstiloCelda(x, alterna)).Text(valor).FontSize(8).FontColor("#1D1D1F");
            }
        });

    private static IContainer EstiloCabecera(IContainer contenedor) =>
        contenedor.BorderBottom(2).BorderColor("#EAB308").PaddingVertical(4).PaddingHorizontal(2);

    private static IContainer EstiloCelda(IContainer contenedor, bool alterna) =>
        contenedor.BorderBottom(1).BorderColor("#E7E7EA").Background(alterna ? "#F8FAFC" : "#FFFFFF").PaddingVertical(4).PaddingHorizontal(2);

    // ---------- MÉTODOS DE EXPORTACIÓN EXCEL ----------

    public async Task<byte[]> GenerarProductosEstablesExcelAsync(
        ReporteProductoFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var filas = await _repositorio.ObtenerProductosEstablesAsync(filtros, cancellationToken);
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Productos Stock");

        hoja.Cell("A1").Value = "FERRETERÍA JOEL";
        hoja.Cell("A1").Style.Font.Bold = true;
        hoja.Cell("A1").Style.Font.FontSize = 14;
        hoja.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1E293B");

        hoja.Cell("A2").Value = "PRODUCTOS DE STOCK ESTABLE";
        hoja.Cell("A2").Style.Font.Bold = true;
        hoja.Cell("A3").Value = $"Filtros: Precio tope Bs {filtros.PrecioMaximo:N2} · Emitido: {DateTime.Now:dd/MM/yyyy HH:mm} · Responsable: {usuario}";
        hoja.Cell("A3").Style.Font.FontColor = XLColor.Gray;

        var cabeceras = new[] { "Código", "Nombre", "Precio", "Descripción", "Marca", "Categoría" };
        for (int i = 0; i < cabeceras.Length; i++)
        {
            hoja.Cell(5, i + 1).Value = cabeceras[i];
            hoja.Cell(5, i + 1).Style.Font.Bold = true;
            hoja.Cell(5, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E40AF");
            hoja.Cell(5, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int fila = 6;
        foreach (var f in filas)
        {
            bool alterna = fila % 2 == 1;
            hoja.Cell(fila, 1).Value = f.Codigo;
            hoja.Cell(fila, 2).Value = f.Nombre;
            hoja.Cell(fila, 3).Value = f.Precio;
            hoja.Cell(fila, 4).Value = f.Descripcion;
            hoja.Cell(fila, 5).Value = f.Marca;
            hoja.Cell(fila, 6).Value = f.Categoria;

            hoja.Cell(fila, 3).Style.NumberFormat.Format = "\"Bs \"#,##0.00";

            var rango = hoja.Range(fila, 1, fila, 6);
            if (alterna) rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            rango.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            rango.Style.Border.BottomBorderColor = XLColor.FromHtml("#E7E7EA");
            fila++;
        }
        hoja.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerarVentasProductoExcelAsync(
        ReporteVentaProductoFiltros filtros, string usuario, CancellationToken cancellationToken = default)
    {
        var ventas = await _repositorio.ObtenerVentasPorProductoAsync(filtros, cancellationToken);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Ventas Producto");

        hoja.Cell("A1").Value = "FERRETERÍA JOEL";
        hoja.Cell("A1").Style.Font.Bold = true;
        hoja.Cell("A1").Style.Font.FontSize = 14;
        hoja.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1E293B");

        hoja.Cell("A2").Value = "VENTAS POR PRODUCTO Y MEDIO DE COBRO";
        hoja.Cell("A2").Style.Font.Bold = true;
        hoja.Cell("A3").Value = $"Filtros: Medio de cobro {filtros.MetodoPago} · Emitido: {DateTime.Now:dd/MM/yyyy HH:mm} · Responsable: {usuario}";
        hoja.Cell("A3").Style.Font.FontColor = XLColor.Gray;

        var cabeceras = new[] { "Nombre", "CantidadTotal", "TotalRecaudado" };
        for (int i = 0; i < cabeceras.Length; i++)
        {
            hoja.Cell(5, i + 1).Value = cabeceras[i];
            hoja.Cell(5, i + 1).Style.Font.Bold = true;
            hoja.Cell(5, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E40AF");
            hoja.Cell(5, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int fila = 6;
        foreach (var v in ventas)
        {
            bool alterna = fila % 2 == 1;
            hoja.Cell(fila, 1).Value = v.Nombre;
            hoja.Cell(fila, 2).Value = v.Cantidad;
            hoja.Cell(fila, 3).Value = v.TotalProducto;

            hoja.Cell(fila, 2).Style.NumberFormat.Format = "#,##0";
            hoja.Cell(fila, 3).Style.NumberFormat.Format = "\"Bs \"#,##0.00";

            var rango = hoja.Range(fila, 1, fila, 3);
            if (alterna) rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            rango.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            rango.Style.Border.BottomBorderColor = XLColor.FromHtml("#E7E7EA");
            fila++;
        }

        // Gráfico de torta. La imagen vectorial solo dibuja las porciones; la
        // leyenda se escribe en celdas porque el texto incrustado en el SVG no
        // se rasteriza de forma fiable en todos los sistemas operativos.
        var segmentos = PrepararSegmentos(ventas);
        hoja.Columns().AdjustToContents();

        if (segmentos.Count > 0)
        {
            hoja.Cell(5, 6).Value = "Participación por producto";
            hoja.Cell(5, 6).Style.Font.Bold = true;
            hoja.Cell(5, 6).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

            int filaLeyenda = 6;
            foreach (SegmentoTorta segmento in segmentos)
            {
                hoja.Cell(filaLeyenda, 5).Style.Fill.BackgroundColor = XLColor.FromHtml(segmento.Color);
                hoja.Cell(filaLeyenda, 6).Value = $"{segmento.Nombre} ({segmento.Porcentaje:N2}%)";
                hoja.Cell(filaLeyenda, 6).Style.Font.FontSize = 10;
                filaLeyenda++;
            }

            // Columna E = muestra de color; F = nombre y porcentaje.
            hoja.Column(5).Width = 3;
            hoja.Column(6).Width = 38;

            try
            {
                string svg = GenerarSvgTorta(segmentos);
                var doc = Document.Create(d => d.Page(p =>
                {
                    p.ContinuousSize(620);
                    p.Content().Svg(svg);
                }));

                byte[] img = doc.GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = 144 }).First();

                using var ms = new MemoryStream(img);
                var pic = hoja.AddPicture(ms);
                pic.MoveTo(hoja.Cell("H5"));
                pic.Width = 520;
                pic.Height = 270;
            }
            catch { /* Si el SO no puede rasterizar, la leyenda en celdas sigue visible. */ }
        }

        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        return stream.ToArray();
    }

    // ---------- GRÁFICO SVG VECTORIAL PARA TORTAS ----------

    private static readonly string[] ColoresTorta =
    {
        "#EAB308", "#2563EB", "#16A34A", "#DC2626", "#9333EA",
        "#0891B2", "#EA580C", "#4F46E5", "#65A30D", "#DB2777"
    };

    private sealed record SegmentoTorta(string Nombre, decimal Valor, decimal Porcentaje, string Color);

    /// <summary>
    /// Prepara hasta 10 porciones ordenadas por recaudación. La leyenda se
    /// dibuja con controles nativos (PDF) o celdas (Excel) para no depender del
    /// soporte de texto dentro del SVG.
    /// </summary>
    private static List<SegmentoTorta> PrepararSegmentos(IReadOnlyList<VentaProducto> filas)
    {
        var topFilas = filas.OrderByDescending(f => f.TotalProducto).Take(ColoresTorta.Length).ToList();
        decimal total = topFilas.Sum(f => f.TotalProducto);
        if (total <= 0) return new List<SegmentoTorta>();

        return topFilas.Select((f, i) => new SegmentoTorta(
            f.Nombre,
            f.TotalProducto,
            Math.Round(f.TotalProducto * 100m / total, 2),
            ColoresTorta[i % ColoresTorta.Length])).ToList();
    }

    private static void LeyendaTorta(IContainer contenedor, IReadOnlyList<SegmentoTorta> segmentos) =>
        contenedor.Column(col =>
        {
            col.Item().Text("Participación por producto").FontSize(9).Bold().FontColor("#1D1D1F");
            foreach (SegmentoTorta segmento in segmentos)
            {
                col.Item().PaddingTop(4).Row(fila =>
                {
                    fila.AutoItem().Width(10).Height(10).Background(segmento.Color);
                    fila.RelativeItem().PaddingLeft(6).Text(texto =>
                    {
                        string nombre = segmento.Nombre.Length > 34
                            ? segmento.Nombre[..34] + "..."
                            : segmento.Nombre;
                        texto.Span(nombre).FontSize(8).FontColor("#1D1D1F");
                        texto.Span($" · {segmento.Porcentaje:N2}%").FontSize(8).Bold().FontColor("#5F5F66");
                    });
                });
            }
        });

    /// <summary>
    /// SVG con las porciones de la torta y sin elementos de texto: algunos
    /// rasterizadores eliminan las etiquetas, por eso la leyenda se dibuja
    /// aparte.
    /// </summary>
    private static string GenerarSvgTorta(IReadOnlyList<SegmentoTorta> segmentos)
    {
        decimal total = segmentos.Sum(s => s.Valor);

        double centroX = 200, centroY = 160, radio = 140;
        StringBuilder svg = new();
        svg.Append("<svg xmlns='http://www.w3.org/2000/svg' width='420' height='320' viewBox='0 0 420 320'>");

        if (segmentos.Count == 0 || total <= 0)
        {
            svg.Append("</svg>");
            return svg.ToString();
        }

        double inicio = -Math.PI / 2;
        for (int i = 0; i < segmentos.Count; i++)
        {
            double proporcion = (double)(segmentos[i].Valor / total);
            double fin = inicio + proporcion * Math.PI * 2;
            double x1 = centroX + radio * Math.Cos(inicio), y1 = centroY + radio * Math.Sin(inicio);
            double x2 = centroX + radio * Math.Cos(fin), y2 = centroY + radio * Math.Sin(fin);

            int arcoGrande = fin - inicio > Math.PI ? 1 : 0;
            string d = $"M {centroX.ToString(CultureInfo.InvariantCulture)} {centroY.ToString(CultureInfo.InvariantCulture)} " +
                       $"L {x1.ToString(CultureInfo.InvariantCulture)} {y1.ToString(CultureInfo.InvariantCulture)} " +
                       $"A {radio} {radio} 0 {arcoGrande} 1 {x2.ToString(CultureInfo.InvariantCulture)} {y2.ToString(CultureInfo.InvariantCulture)} Z";

            svg.Append($"<path d='{d}' fill='{segmentos[i].Color}' stroke='white' stroke-width='2'/>");
            inicio = fin;
        }

        svg.Append("</svg>");
        return svg.ToString();
    }
}