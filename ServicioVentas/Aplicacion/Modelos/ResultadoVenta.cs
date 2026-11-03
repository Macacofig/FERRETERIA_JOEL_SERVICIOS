using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Aplicacion.Modelos
{
    /// <summary>
    /// Resultado de una operación de venta (registro, consulta o
    /// anulación). Nunca lanza excepciones de negocio: informa el
    /// desenlace mediante <see cref="Exito"/> y <see cref="Mensaje"/>.
    /// </summary>
    public class ResultadoVenta
    {
        public bool Exito { get; private init; }

        public string Mensaje { get; private init; } = string.Empty;

        public int IdVenta { get; private init; }

        public string? CodigoVenta { get; private init; }

        public int IdCliente { get; private init; }

        public string MetodoPago { get; private init; } = string.Empty;

        public string EstadoPago { get; private init; } = string.Empty;

        public string EstadoVenta { get; private init; } = string.Empty;

        public decimal MontoTotal { get; private init; }

        public DateTime FechaRegistro { get; private init; }

        public DateTime? FechaAnulacion { get; private init; }

        public string? MotivoAnulacion { get; private init; }

        public List<LineaVentaDetalle> Detalles { get; private init; } = new();

        public static ResultadoVenta Correcto(
            Venta venta,
            IEnumerable<LineaVentaDetalle> detalles)
        {
            return Construir(venta, detalles);
        }

        public static ResultadoVenta Fallido(string mensaje)
        {
            return new ResultadoVenta
            {
                Exito = false,
                Mensaje = mensaje
            };
        }

        private static ResultadoVenta Construir(
            Venta venta,
            IEnumerable<LineaVentaDetalle> detalles)
        {
            return new ResultadoVenta
            {
                Exito = true,
                Mensaje = "Venta registrada correctamente.",
                IdVenta = venta.IdVenta,
                CodigoVenta = venta.CodigoVenta,
                IdCliente = venta.IdCliente,
                MetodoPago = venta.MetodoPago,
                EstadoPago = venta.EstadoPago,
                EstadoVenta = venta.EstadoVenta,
                MontoTotal = venta.MontoTotal,
                FechaRegistro = venta.FechaRegistro,
                FechaAnulacion = venta.FechaAnulacion,
                MotivoAnulacion = venta.MotivoAnulacion,
                Detalles = detalles.ToList()
            };
        }
    }
}
