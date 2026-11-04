namespace FERRETERIA__Joel.Modelos
{
    /// <summary>
    /// Línea que el formulario de venta envía al manejador. El precio y los
    /// totales nunca viajan desde el navegador: los resuelve ServicioVentas.
    /// </summary>
    public class LineaVentaSolicitada
    {
        public int IdProducto { get; set; }

        public int Cantidad { get; set; }
    }
}
