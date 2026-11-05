using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.Servicios;
using ServicioVentas.Dominio.Constantes;
using ServicioVentas.Dominio.Utilidades;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ServicioVentas.Servicios
{
    /// <summary>
    /// Adaptador del puerto <see cref="IGeneradorComprobantePdf"/> basado en
    /// QuestPDF. Reproduce el diseño del comprobante en pantalla: marca,
    /// datos generales, detalle, totales, literal, fecha/hora de generación
    /// y usuario que lo generó.
    /// </summary>
    public class QuestPdfGeneradorComprobante : IGeneradorComprobantePdf
    {
        private const string ColorMarca = "#FFC824";
        private const string ColorMarcaTexto = "#A16207";
        private const string ColorBorde = "#D1D5DB";
        private const string ColorTextoSecundario = "#6B7280";
        private const string ColorAnulada = "#B91C1C";

        static QuestPdfGeneradorComprobante()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] Generar(ComprobanteVenta comprobante)
        {
            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4);
                    pagina.Margin(36);
                    pagina.DefaultTextStyle(x => x.FontSize(10));

                    pagina.Header().Element(c => ComponerCabecera(c, comprobante));
                    pagina.Content().Element(c => ComponerContenido(c, comprobante));
                    pagina.Footer().Element(c => ComponerPie(c, comprobante));
                });
            }).GeneratePdf();
        }

        private static void ComponerCabecera(IContainer contenedor, ComprobanteVenta comprobante)
        {
            var venta = comprobante.Venta;

            contenedor.PaddingBottom(10).BorderBottom(1).BorderColor(ColorBorde).Row(fila =>
            {
                fila.RelativeItem().Column(col =>
                {
                    col.Item().Text(texto =>
                    {
                        texto.Span("FERRETERÍA ").Bold().FontSize(18);
                        texto.Span("JOEL").Bold().FontSize(18).FontColor(ColorMarcaTexto);
                    });
                    col.Item().Text("Av. Central N.º 123, El Alto · La Paz")
                        .FontSize(9).FontColor(ColorTextoSecundario);
                });

                fila.ConstantItem(190).AlignRight().Column(col =>
                {
                    col.Item().AlignRight().Text("COMPROBANTE DE VENTA").Bold().FontSize(12);
                    col.Item().AlignRight().Text(venta.CodigoVenta ?? string.Empty);
                    col.Item().AlignRight().Text(venta.FechaRegistro.ToString("dd/MM/yyyy HH:mm"));
                });
            });
        }

        private static void ComponerContenido(IContainer contenedor, ComprobanteVenta comprobante)
        {
            var venta = comprobante.Venta;
            bool registrada = venta.EstadoVenta == EstadoVenta.Registrada;

            contenedor.PaddingTop(14).Column(col =>
            {
                col.Spacing(12);

                col.Item().Element(c => ComponerDatosGenerales(c, comprobante, registrada));
                col.Item().Element(c => ComponerDetalle(c, comprobante));
                col.Item().Element(c => ComponerTotales(c, venta));

                col.Item().Text(texto =>
                {
                    texto.Span("Son: ").Bold();
                    texto.Span(NumeroALetras.EnLetras(venta.MontoTotal));
                });

                if (!registrada)
                {
                    col.Item().Border(1).BorderColor(ColorAnulada).Padding(8).Column(anulada =>
                    {
                        anulada.Item().Text("VENTA ANULADA").Bold().FontColor(ColorAnulada);
                        anulada.Item().Text($"Motivo: {venta.MotivoAnulacion}");
                        anulada.Item().Text(
                            $"Anulada el {venta.FechaAnulacion:dd/MM/yyyy HH:mm}"
                            + (string.IsNullOrWhiteSpace(comprobante.NombreEmpleadoAnulacion)
                                ? string.Empty
                                : $" por {NombreAbreviado.De(comprobante.NombreEmpleadoAnulacion)}"));
                    });
                }
            });
        }

        private static void ComponerDatosGenerales(
            IContainer contenedor, ComprobanteVenta comprobante, bool registrada)
        {
            var venta = comprobante.Venta;

            contenedor.Table(tabla =>
            {
                tabla.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                Dato(tabla, "Cliente", string.IsNullOrWhiteSpace(comprobante.CiCliente)
                    ? comprobante.NombreCliente
                    : $"{comprobante.NombreCliente}\nCI {comprobante.CiCliente}");
                Dato(tabla, "Método de pago", venta.MetodoPago);
                Dato(tabla, "Estado del pago", venta.EstadoPago);
                Dato(tabla, "Generado por", NombreAbreviado.De(comprobante.NombreEmpleadoResponsable));
                Dato(tabla, "Fecha de emisión", venta.FechaRegistro.ToString("dd/MM/yyyy HH:mm"));
                Dato(tabla, "Estado", registrada ? "Registrada" : "Anulada");
            });
        }

        private static void Dato(TableDescriptor tabla, string etiqueta, string valor)
        {
            tabla.Cell().PaddingBottom(6).Column(col =>
            {
                col.Item().Text(etiqueta).FontSize(8).FontColor(ColorTextoSecundario);
                col.Item().Text(valor).SemiBold();
            });
        }

        private static void ComponerDetalle(IContainer contenedor, ComprobanteVenta comprobante)
        {
            contenedor.Table(tabla =>
            {
                tabla.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(5);
                    c.RelativeColumn(1.6f);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                });

                tabla.Header(cabecera =>
                {
                    cabecera.Cell().Element(EstiloCabecera).Text("Producto").Bold();
                    cabecera.Cell().Element(EstiloCabecera).Text("Und.").Bold();
                    cabecera.Cell().Element(EstiloCabecera).AlignRight().Text("Precio").Bold();
                    cabecera.Cell().Element(EstiloCabecera).AlignRight().Text("Importe").Bold();
                });

                foreach (var linea in comprobante.Lineas)
                {
                    tabla.Cell().Element(EstiloCelda).Column(col =>
                    {
                        col.Item().Text(linea.Nombre).SemiBold();
                        col.Item().Text(linea.Codigo).FontSize(8).FontColor(ColorTextoSecundario);
                    });
                    tabla.Cell().Element(EstiloCelda).Text($"{linea.Cantidad} {linea.UnidadMedida}");
                    tabla.Cell().Element(EstiloCelda).AlignRight().Text($"{linea.PrecioUnitario:N2} Bs");
                    tabla.Cell().Element(EstiloCelda).AlignRight().Text($"{linea.TotalProducto:N2} Bs");
                }
            });
        }

        private static void ComponerTotales(IContainer contenedor, Dominio.Entidades.Venta venta)
        {
            contenedor.AlignRight().Width(230).Column(col =>
            {
                col.Item().PaddingTop(4).BorderTop(1).BorderColor(ColorBorde).Row(f =>
                {
                    f.RelativeItem().Text("TOTAL").Bold().FontSize(12);
                    f.AutoItem().Text($"{venta.MontoTotal:N2} Bs").Bold().FontSize(12);
                });
            });
        }

        private static void ComponerPie(IContainer contenedor, ComprobanteVenta comprobante)
        {
            contenedor.BorderTop(1).BorderColor(ColorBorde).PaddingTop(6).Row(fila =>
            {
                fila.RelativeItem().Text(
                    $"Generado el {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} · {NombreAbreviado.De(comprobante.NombreEmpleadoResponsable)}")
                    .FontSize(8).FontColor(ColorTextoSecundario);

                fila.AutoItem().Text(texto =>
                {
                    texto.DefaultTextStyle(x => x.FontSize(8).FontColor(ColorTextoSecundario));
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        }

        private static IContainer EstiloCabecera(IContainer c) =>
            c.Background(ColorMarca).PaddingVertical(5).PaddingHorizontal(6);

        private static IContainer EstiloCelda(IContainer c) =>
            c.BorderBottom(1).BorderColor(ColorBorde).PaddingVertical(5).PaddingHorizontal(6);
    }
}
