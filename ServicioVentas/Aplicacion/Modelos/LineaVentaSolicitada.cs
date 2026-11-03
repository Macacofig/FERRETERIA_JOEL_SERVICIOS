namespace ServicioVentas.Aplicacion.Modelos
{
    /// <summary>
    /// Línea solicitada por la capa de presentación. El precio y los
    /// totales nunca llegan desde el cliente: los resuelve la fachada
    /// a partir del catálogo.
    /// </summary>
    public class LineaVentaSolicitada
    {
        public int IdProducto { get; set; }

        public int Cantidad { get; set; }
    }
}
