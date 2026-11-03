namespace ServicioVentas.Aplicacion.Modelos
{
    /// <summary>
    /// Línea de venta calculada por la fachada. Se usa tanto para la
    /// previsualización en pantalla como para el comprobante.
    /// </summary>
    public class LineaVentaDetalle
    {
        public int IdProducto { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string UnidadMedida { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal TotalProducto { get; set; }

        public int CantidadStock { get; set; }
    }
}
