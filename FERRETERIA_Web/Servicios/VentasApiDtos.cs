namespace FERRETERIA__Joel.Servicios
{
    /// <summary>
    /// DTOs que el front usa para hablar con ServicioVentas. Son una copia
    /// deliberada del contrato HTTP: la capa Razor no comparte entidades ni
    /// repositorios con la API, solo JSON con el JWT en la cabecera.
    /// </summary>
    public class ClienteBusquedaApi
    {
        public int Id { get; set; }

        public string Ci { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;
    }

    public class ProductoBusquedaApi
    {
        public int Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string Unidad { get; set; } = string.Empty;

        public decimal Precio { get; set; }

        public int Stock { get; set; }
    }

    public class ProductoCatalogoApi
    {
        public int Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string? Marca { get; set; }

        public string? Categoria { get; set; }

        public string? Descripcion { get; set; }

        public string Unidad { get; set; } = string.Empty;

        public decimal Precio { get; set; }

        public int Stock { get; set; }

        public byte Estado { get; set; }
    }

    public class LineaVentaApi
    {
        public int IdProducto { get; set; }

        public int Cantidad { get; set; }
    }

    public class LineaDetalleApi
    {
        public string Nombre { get; set; } = string.Empty;

        public string Codigo { get; set; } = string.Empty;

        public string Unidad { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public decimal Precio { get; set; }

        public decimal Total { get; set; }
    }

    public class VentaApi
    {
        public int IdVenta { get; set; }

        public string? CodigoVenta { get; set; }

        public string EstadoVenta { get; set; } = string.Empty;

        public int IdCliente { get; set; }

        public string MetodoPago { get; set; } = string.Empty;

        public string EstadoPago { get; set; } = string.Empty;

        public decimal MontoTotal { get; set; }

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaAnulacion { get; set; }

        public string? MotivoAnulacion { get; set; }
    }

    public class VentaListadoApi
    {
        public VentaApi Venta { get; set; } = new();

        public string NombreCliente { get; set; } = string.Empty;
    }

    public class DetalleVentaApi
    {
        public string? CodigoVenta { get; set; }

        public string EstadoVenta { get; set; } = string.Empty;

        public string MetodoPago { get; set; } = string.Empty;

        public string EstadoPago { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public decimal Total { get; set; }

        public string? MotivoAnulacion { get; set; }

        public List<LineaDetalleApi> Lineas { get; set; } = new();
    }

    public class ComprobanteVentaApi
    {
        public VentaApi Venta { get; set; } = new();

        public string NombreCliente { get; set; } = string.Empty;

        public string CiCliente { get; set; } = string.Empty;

        public string NombreEmpleadoResponsable { get; set; } = string.Empty;

        public string? NombreEmpleadoAnulacion { get; set; }

        public List<LineaDetalleApi> Lineas { get; set; } = new();
    }

    public class ResultadoVentaApi
    {
        public bool Exito { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public int Id { get; set; }

        public string Ci { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public int IdVenta { get; set; }

        public string? CodigoVenta { get; set; }

        public decimal MontoTotal { get; set; }

        public List<LineaDetalleApi> Detalles { get; set; } = new();

        public Dictionary<string, string> Errores { get; set; } = new();
    }

    public class RegistrarVentaApi
    {
        public int IdCliente { get; set; }

        public string MetodoPago { get; set; } = string.Empty;

        public string EstadoPago { get; set; } = string.Empty;

        public List<LineaVentaApi> Lineas { get; set; } = new();
    }

    public class CrearClienteApi
    {
        public string? Ci { get; set; }

        public string? Nombre { get; set; }

        public string? Apellido { get; set; }
    }

    public class AnularVentaApi
    {
        public int IdVenta { get; set; }

        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Error controlado devuelto por <see cref="VentasApiCliente"/> cuando la
    /// API responde con un código no exitoso. Conserva el status HTTP para
    /// que las páginas puedan reflejarlo al navegador.
    /// </summary>
    public class VentasApiException : Exception
    {
        public VentasApiException(int estado, string mensaje)
            : base(mensaje)
        {
            Estado = estado;
        }

        public int Estado { get; }
    }
}
