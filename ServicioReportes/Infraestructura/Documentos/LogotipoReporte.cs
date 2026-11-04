using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ServicioReportes.Infraestructura.Documentos
{
    /// <summary>
    /// Genera en memoria la marca institucional "FJ" usada como logotipo en
    /// el membrete de PDF y Excel. Se dibuja con QuestPDF para no depender de
    /// archivos binarios versionados y se cachea al primer uso.
    /// </summary>
    public static class LogotipoReporte
    {
        private static readonly Lazy<byte[]> Imagen = new(
            CrearImagen,
            LazyThreadSafetyMode.ExecutionAndPublication);

        public static byte[] ObtenerPng()
        {
            return Imagen.Value;
        }

        private static byte[] CrearImagen()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(180, 180);
                    pagina.Margin(0);
                    pagina.DefaultTextStyle(x => x.FontColor(PaletaReporte.Texto));

                    pagina.Content()
                        .Background(PaletaReporte.Amarillo)
                        .Padding(12)
                        .Column(columna =>
                        {
                            columna.Item()
                                .AlignCenter()
                                .AlignMiddle()
                                .Text(texto =>
                                {
                                    texto.Span("FJ").Bold().FontSize(66);
                                });

                            columna.Item()
                                .AlignCenter()
                                .Text(texto =>
                                {
                                    texto.Span("FERRETERÍA")
                                        .Bold()
                                        .FontSize(11)
                                        .LetterSpacing(0.08f);
                                });
                        });
                });
            }).GenerateImages(new ImageGenerationSettings
            {
                ImageFormat = ImageFormat.Png,
                RasterDpi = 220
            }).First();
        }
    }
}
