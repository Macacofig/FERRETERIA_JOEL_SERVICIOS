using ClosedXML.Excel;
using ServicioReportes.Aplicacion.Modelos.Reportes;
using ServicioReportes.Aplicacion.Puertos;

namespace ServicioReportes.Infraestructura.Documentos;

/// <summary>Adaptador ClosedXML para exportar los reportes a Excel con diseño limpio y sin congelamientos.</summary>
public sealed class ClosedXmlGeneradorReportes : IGeneradorReporteExcel
{
    private const string FuenteCompatibleDocker = "DejaVu Sans";

    public byte[] GenerarVentas(ReporteVentas reporte)
    {
        using XLWorkbook libro = new();
        IXLWorksheet hoja = libro.Worksheets.Add("Productos stock");
        const int primeraColumna = 1;
        const int ultimaColumna = 7;

        // Membrete iniciando en A1 con Logo en la esquina superior derecha (Columna F)
        Membrete(hoja, ultimaColumna, reporte.Titulo, reporte.Filtro.FiltroTexto, reporte.GeneradoPor, reporte.GeneradoEl, "F1");

        // Encabezado de la tabla en la fila 6
        Encabezado(hoja, 6, primeraColumna, new[] { "Código", "Producto", "Precio", "Descripción", "Marca", "Categoría", "Stock" });

        int fila = 7;
        foreach (LineaReporteVentas linea in reporte.Lineas)
        {
            hoja.Cell(fila, 1).Value = linea.Codigo;
            hoja.Cell(fila, 2).Value = linea.Producto;
            hoja.Cell(fila, 3).Value = linea.Precio;
            hoja.Cell(fila, 4).Value = linea.Descripcion;
            hoja.Cell(fila, 5).Value = linea.Marca;
            hoja.Cell(fila, 6).Value = linea.Categoria;
            hoja.Cell(fila, 7).Value = linea.CantidadStock;

            hoja.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            hoja.Cell(fila, 3).Style.NumberFormat.Format = "\"Bs \"#,##0.00";
            hoja.Cell(fila, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            hoja.Cell(fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            hoja.Cell(fila, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 7).Style.NumberFormat.Format = "#,##0";
            hoja.Cell(fila, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            EstiloFila(hoja, fila, primeraColumna, ultimaColumna, fila % 2 == 0);
            fila++;
        }

        int filaTotal = fila;
        hoja.Cell(filaTotal, 6).Value = "TOTAL UNIDADES:";
        hoja.Cell(filaTotal, 6).Style.Font.Bold = true;
        hoja.Cell(filaTotal, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        if (reporte.Lineas.Count > 0)
        {
            hoja.Cell(filaTotal, 7).FormulaA1 = $"=SUM(G7:G{filaTotal - 1})";
        }
        else
        {
            hoja.Cell(filaTotal, 7).Value = 0;
        }
        hoja.Cell(filaTotal, 7).Style.NumberFormat.Format = "#,##0";
        EstiloTotal(hoja.Range(filaTotal, primeraColumna, filaTotal, ultimaColumna));

        AjustarColumnas(hoja, primeraColumna, ultimaColumna);
        ConfigurarPagina(hoja, "Ferretería Joel", reporte.GeneradoEl);

        hoja.ShowGridLines = true;
        // SIN freeze panes: navegación 100% fluida
        return Guardar(libro);
    }

    public byte[] GenerarEstadisticas(ReporteEstadisticas reporte)
    {
        using XLWorkbook libro = new();
        IXLWorksheet hoja = libro.Worksheets.Add("Ventas por producto");
        const int primeraColumna = 1;
        const int ultimaColumnaTabla = 5;

        // Membrete en Columna A, con Logo en la parte superior derecha (Columna E)
        Membrete(hoja, 5, reporte.Titulo, reporte.Filtro.FiltroTexto, reporte.GeneradoPor, reporte.GeneradoEl, "E1");

        // Tarjetas compactas de resumen (KPIs) en filas 4 y 5
        hoja.Range("A4:B4").Merge().Value = $"PRODUCTOS: {reporte.TotalProductos:N0}";
        hoja.Range("C4:D4").Merge().Value = $"UNIDADES: {reporte.TotalUnidades:N0}";
        hoja.Cell("E4").Value = $"TOTAL: Bs {reporte.TotalImporte:N2}";

        foreach (IXLRange kpi in new[] { hoja.Range("A4:B4"), hoja.Range("C4:D4"), hoja.Range("E4:E4") })
        {
            kpi.Style.Font.Bold = true;
            kpi.Style.Font.FontSize = 10;
            kpi.Style.Fill.BackgroundColor = XLColor.FromHtml(PaletaReporte.FondoSuave);
            kpi.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            kpi.Style.Border.OutsideBorderColor = XLColor.FromHtml(PaletaReporte.BordeSuave);
            kpi.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            kpi.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        hoja.Row(4).Height = 22;

        // Cabecera de la tabla en fila 6 (Columnas A a E)
        Encabezado(hoja, 6, primeraColumna, new[] { "N.°", "Producto", "Cantidad vendida", "Total recaudado", "Participación" });

        int fila = 7;
        int numero = 1;
        foreach (LineaEstadisticaCategoria linea in reporte.FilasOrdenadas)
        {
            hoja.Cell(fila, 1).Value = numero++;
            hoja.Cell(fila, 2).Value = linea.Producto;
            hoja.Cell(fila, 3).Value = linea.CantidadTotal;
            hoja.Cell(fila, 4).Value = linea.TotalRecaudado;
            hoja.Cell(fila, 5).Value = linea.Participacion / 100m;

            hoja.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            hoja.Cell(fila, 3).Style.NumberFormat.Format = "#,##0";
            hoja.Cell(fila, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            hoja.Cell(fila, 4).Style.NumberFormat.Format = "\"Bs \"#,##0.00";
            hoja.Cell(fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            hoja.Cell(fila, 5).Style.NumberFormat.Format = "0.00%";
            hoja.Cell(fila, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            EstiloFila(hoja, fila, primeraColumna, ultimaColumnaTabla, fila % 2 == 0);
            fila++;
        }

        int filaTotal = fila;
        hoja.Cell(filaTotal, 1).Value = "TOTAL";
        hoja.Cell(filaTotal, 3).Value = reporte.TotalUnidades;
        hoja.Cell(filaTotal, 4).Value = reporte.TotalImporte;
        hoja.Cell(filaTotal, 5).Value = 1m;

        hoja.Cell(filaTotal, 3).Style.NumberFormat.Format = "#,##0";
        hoja.Cell(filaTotal, 4).Style.NumberFormat.Format = "\"Bs \"#,##0.00";
        hoja.Cell(filaTotal, 5).Style.NumberFormat.Format = "0.00%";
        EstiloTotal(hoja.Range(filaTotal, primeraColumna, filaTotal, ultimaColumnaTabla));

        // Gráfico de torta a la derecha (Columna G, fila 6) con tamaño visible y proporción cómoda (560x300 px)
        try
        {
            using MemoryStream imagen = new(GraficoPieReporte.GenerarPng(reporte));
            var picture = hoja.AddPicture(imagen)
                              .MoveTo(hoja.Cell("G6"));
            picture.Width = 560;
            picture.Height = 300;
        }
        catch
        {
            // Fallback silencioso si no hay datos o falla el renderizado
        }

        AjustarColumnas(hoja, primeraColumna, ultimaColumnaTabla);
        ConfigurarPagina(hoja, "Ferretería Joel", reporte.GeneradoEl);

        hoja.ShowGridLines = true;
        // SIN freeze panes: desplazamiento totalmente libre sin paneles bloqueados
        return Guardar(libro);
    }

    private static void Membrete(IXLWorksheet hoja, int ultimaColumna, string titulo, string filtros, string usuario, DateTime fecha, string celdaLogo)
    {
        hoja.Style.Font.FontName = FuenteCompatibleDocker;
        hoja.Row(1).Height = 24;
        hoja.Row(2).Height = 20;
        hoja.Row(3).Height = 18;

        // Textos alineados a la izquierda desde la Columna A
        hoja.Cell("A1").Value = "FERRETERÍA JOEL";
        hoja.Cell("A1").Style.Font.Bold = true;
        hoja.Cell("A1").Style.Font.FontSize = 13;
        hoja.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1E293B");

        hoja.Cell("A2").Value = titulo;
        hoja.Cell("A2").Style.Font.Bold = true;
        hoja.Cell("A2").Style.Font.FontSize = 11;

        hoja.Cell("A3").Value = $"Filtros: {filtros} · Emitido: {fecha:dd/MM/yyyy HH:mm} · Responsable: {usuario}";
        hoja.Cell("A3").Style.Font.FontColor = XLColor.FromHtml(PaletaReporte.TextoSecundario);
        hoja.Cell("A3").Style.Font.FontSize = 9;

        // Inserción controlada del logo en la esquina derecha sin tapar texto
        try
        {
            using MemoryStream logoStream = new(LogotipoReporte.ObtenerPng());
            var logo = hoja.AddPicture(logoStream)
                           .MoveTo(hoja.Cell(celdaLogo));
            logo.Width = 64;
            logo.Height = 64;
        }
        catch
        {
            // Continuar si el logo no está disponible en el entorno
        }
    }

    private static void Encabezado(IXLWorksheet hoja, int fila, int primeraColumna, string[] titulos)
    {
        for (int indice = 0; indice < titulos.Length; indice++)
        {
            hoja.Cell(fila, primeraColumna + indice).Value = titulos[indice];
        }
        IXLRange rango = hoja.Range(fila, primeraColumna, fila, primeraColumna + titulos.Length - 1);
        rango.Style.Font.Bold = true;
        rango.Style.Font.FontColor = XLColor.White;
        rango.Style.Fill.BackgroundColor = XLColor.FromHtml(PaletaReporte.Azul);
        rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        rango.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        hoja.Row(fila).Height = 24;
    }

    private static void EstiloFila(IXLWorksheet hoja, int fila, int primeraColumna, int ultimaColumna, bool alterna)
    {
        IXLRange rango = hoja.Range(fila, primeraColumna, fila, ultimaColumna);
        if (alterna) rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        rango.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        rango.Style.Border.BottomBorderColor = XLColor.FromHtml(PaletaReporte.BordeSuave);
        hoja.Row(fila).Height = 20;
    }

    private static void EstiloTotal(IXLRange rango)
    {
        rango.Style.Font.Bold = true;
        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
        rango.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        rango.Style.Border.BottomBorder = XLBorderStyleValues.Double;
        rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void AjustarColumnas(IXLWorksheet hoja, int primeraColumna, int ultimaColumna)
    {
        for (int columna = primeraColumna; columna <= ultimaColumna; columna++)
        {
            hoja.Column(columna).AdjustToContents();
            hoja.Column(columna).Width += 3.5;
        }
    }

    private static void ConfigurarPagina(IXLWorksheet hoja, string nombreEmpresa, DateTime fecha)
    {
        hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        hoja.PageSetup.PaperSize = XLPaperSize.A4Paper;
        hoja.PageSetup.FitToPages(1, 0);
        hoja.PageSetup.Margins.Top = 0.5;
        hoja.PageSetup.Margins.Bottom = 0.5;
        hoja.PageSetup.Margins.Left = 0.4;
        hoja.PageSetup.Margins.Right = 0.4;
        hoja.PageSetup.Header.Left.AddText(nombreEmpresa);
        hoja.PageSetup.Footer.Left.AddText($"Generado el: {fecha:dd/MM/yyyy HH:mm}");
        hoja.PageSetup.Footer.Right.AddText("Página &P de &N");
    }

    private static byte[] Guardar(XLWorkbook libro)
    {
        using MemoryStream salida = new();
        libro.SaveAs(salida);
        return salida.ToArray();
    }
}