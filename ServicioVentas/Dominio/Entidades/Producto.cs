namespace ServicioVentas.Dominio.Entidades
{
    public class Producto
    {
        public int IdProducto { get; set; }
        public int IdCategoria { get; set; }
        public int IdMarca { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? NombreMarca { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public int CantidadStock { get; set; }

        public byte Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public int IdEmpleadoResponsable { get; set; }

        public string? NombreCategoria { get; set; }
    }
}
