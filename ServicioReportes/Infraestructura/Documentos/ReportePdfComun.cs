using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ServicioReportes.Infraestructura.Documentos
{
    /// <summary>
    /// Piezas visuales compartidas por los dos reportes PDF: membrete,
    /// bloque de filtros, estilos de tabla y pie de página con paginación.
    /// </summary>
    public static class ReportePdfComun
    {
        public static void Cabecera(
            IContainer contenedor,
            string titulo,
            string codigo,
            DateTime generadoEl)
        {
            contenedor.Column(columna =>
            {
                columna.Item().Height(5).Background(PaletaReporte.Amarillo);

                columna.Item().PaddingTop(9).Row(fila =>
                {
                    fila.ConstantItem(52)
                        .Height(52)
                        .Image(LogotipoReporte.ObtenerPng())
                        .FitArea();

                    fila.RelativeItem().PaddingLeft(11).Column(marca =>
                    {
                        marca.Item().Text(texto =>
                        {
                            texto.Span("FERRETERÍA ").Bold().FontSize(16);
                            texto.Span("JOEL").Bold().FontSize(16)
                                .FontColor(PaletaReporte.AmarilloTexto);
                        });

                        marca.Item().Text("Herramientas y materiales de construcción")
                            .FontSize(8.5f)
                            .FontColor(PaletaReporte.TextoSecundario);

                        marca.Item().Text(
                                "Av. Central N.º 123, El Alto · La Paz · +591 2 123 4567 · contacto@ferreteriajoel.com")
                            .FontSize(7.5f)
                            .FontColor(PaletaReporte.TextoTerciario);
                    });

                    fila.ConstantItem(210).AlignRight().Column(documento =>
                    {
                        documento.Item().AlignRight().Text(titulo)
                            .Bold().FontSize(10.5f);

                        documento.Item().AlignRight().Text(codigo)
                            .FontSize(7.5f)
                            .FontColor(PaletaReporte.TextoSecundario);

                        documento.Item().AlignRight()
                            .Text($"Emitido: {generadoEl:dd/MM/yyyy HH:mm}")
                            .FontSize(7.5f)
                            .FontColor(PaletaReporte.TextoSecundario);
                    });
                });

                columna.Item().PaddingTop(9)
                    .LineHorizontal(1)
                    .LineColor(PaletaReporte.Borde);
            });
        }

        public static void Titulo(
            IContainer contenedor,
            string seccion,
            string titulo,
            string descripcion)
        {
            contenedor.Column(columna =>
            {
                columna.Item().Text(seccion.ToUpperInvariant())
                    .Bold()
                    .FontSize(7.5f)
                    .FontColor(PaletaReporte.AmarilloTexto)
                    .LetterSpacing(0.08f);

                columna.Item().PaddingTop(2).Text(titulo)
                    .Bold()
                    .FontSize(19);

                columna.Item().PaddingTop(2).Text(descripcion)
                    .FontSize(8.5f)
                    .FontColor(PaletaReporte.TextoSecundario);
            });
        }

        public static void Filtros(
            IContainer contenedor,
            params (string Etiqueta, string Valor)[] filtros)
        {
            contenedor.Background(PaletaReporte.FondoSuave)
                .PaddingVertical(7)
                .PaddingHorizontal(9)
                .Row(fila =>
                {
                    foreach ((string etiqueta, string valor) in filtros)
                    {
                        fila.RelativeItem().Column(columna =>
                        {
                            columna.Item().Text(etiqueta.ToUpperInvariant())
                                .FontSize(6.5f)
                                .FontColor(PaletaReporte.TextoTerciario)
                                .LetterSpacing(0.05f);

                            columna.Item().Text(valor)
                                .FontSize(8)
                                .SemiBold();
                        });
                    }
                });
        }

        public static void Resumen(
            IContainer contenedor,
            IReadOnlyList<(string Etiqueta, string Valor)> valores,
            bool destacarUltimo = true)
        {
            contenedor.Row(fila =>
            {
                for (int indice = 0; indice < valores.Count; indice++)
                {
                    bool destacado = destacarUltimo && indice == valores.Count - 1;

                    fila.RelativeItem()
                        .PaddingRight(indice == valores.Count - 1 ? 0 : 6)
                        .Background(destacado
                            ? PaletaReporte.Amarillo
                            : PaletaReporte.FondoSuave)
                        .Padding(7)
                        .Column(columna =>
                        {
                            columna.Item().Text(valores[indice].Etiqueta.ToUpperInvariant())
                                .FontSize(6.5f)
                                .FontColor(PaletaReporte.TextoSecundario)
                                .LetterSpacing(0.04f);

                            columna.Item().PaddingTop(1)
                                .Text(valores[indice].Valor)
                                .Bold()
                                .FontSize(11);
                        });
                }
            });
        }

        public static void Pie(
            IContainer contenedor,
            string generadoPor,
            DateTime generadoEl,
            string totalResumen)
        {
            contenedor.BorderTop(1)
                .BorderColor(PaletaReporte.Borde)
                .PaddingTop(6)
                .Row(fila =>
                {
                    fila.RelativeItem().Column(columna =>
                    {
                        columna.Item().Text(
                                $"Generado el {generadoEl:dd/MM/yyyy HH:mm:ss} · "
                                + $"Responsable: {generadoPor}")
                            .FontSize(7)
                            .FontColor(PaletaReporte.TextoSecundario);

                        columna.Item().Text(totalResumen)
                            .FontSize(7.5f)
                            .SemiBold();
                    });

                    fila.AutoItem().AlignBottom().Text(texto =>
                    {
                        texto.DefaultTextStyle(x => x
                            .FontSize(7.5f)
                            .FontColor(PaletaReporte.TextoSecundario));

                        texto.Span("Página ");
                        texto.CurrentPageNumber();
                        texto.Span(" de ");
                        texto.TotalPages();
                    });
                });
        }

        public static IContainer EstiloCabecera(IContainer contenedor)
        {
            return contenedor.Background(PaletaReporte.Amarillo)
                .PaddingVertical(5)
                .PaddingHorizontal(5);
        }

        public static IContainer EstiloCelda(IContainer contenedor, bool alterna)
        {
            IContainer celda = contenedor
                .BorderBottom(1)
                .BorderColor(PaletaReporte.BordeSuave)
                .PaddingVertical(4)
                .PaddingHorizontal(5);

            return alterna
                ? celda.Background(PaletaReporte.FondoSuave)
                : celda;
        }

        public static IContainer EstiloTotal(IContainer contenedor)
        {
            return contenedor.Background(PaletaReporte.Amarillo)
                .PaddingVertical(6)
                .PaddingHorizontal(5);
        }
    }
}
