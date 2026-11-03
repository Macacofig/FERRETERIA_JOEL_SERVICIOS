namespace ServicioVentas.Dominio.Entidades
{
    public class DetalleVenta
    {
        public int IdDetalleVenta { get; set; }
        public int IdVenta { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal TotalProducto { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int IdEmpleadoResponsable { get; set; }
    }
}
