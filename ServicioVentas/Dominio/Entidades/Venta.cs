namespace ServicioVentas.Dominio.Entidades
{
    public class Venta
    {
        public int IdVenta { get; set; }
        public string? CodigoVenta { get; set; }
        public string EstadoVenta { get; set; } = "Registrada";
        public int IdCliente { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string EstadoPago { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public string? MotivoAnulacion { get; set; }
        public int? IdEmpleadoAnulacion { get; set; }
        public int IdEmpleadoResponsable { get; set; }
    }
}
