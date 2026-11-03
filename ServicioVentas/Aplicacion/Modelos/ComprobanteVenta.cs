using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Aplicacion.Modelos
{
    /// <summary>
    /// Datos completos del comprobante/recibo de una venta: la venta,
    /// el nombre del cliente y sus líneas enriquecidas con el catálogo.
    /// </summary>
    public class ComprobanteVenta
    {
        public Venta Venta { get; set; } = new();

        public string NombreCliente { get; set; } = string.Empty;

        public string CiCliente { get; set; } = string.Empty;

        public string NombreEmpleadoResponsable { get; set; } = string.Empty;

        public string? NombreEmpleadoAnulacion { get; set; }

        public List<LineaVentaDetalle> Lineas { get; set; } = new();
    }
}
