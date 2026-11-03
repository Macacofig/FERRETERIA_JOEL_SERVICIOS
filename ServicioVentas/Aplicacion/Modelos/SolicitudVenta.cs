namespace ServicioVentas.Aplicacion.Modelos
{
    /// <summary>
    /// Datos de entrada para registrar una venta.
    /// </summary>
    public class SolicitudVenta
    {
        public int IdCliente { get; set; }

        public string MetodoPago { get; set; } = string.Empty;

        public string EstadoPago { get; set; } = string.Empty;

        public List<LineaVentaSolicitada> Lineas { get; set; } = new();
    }
}
