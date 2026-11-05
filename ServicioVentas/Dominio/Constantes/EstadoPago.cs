namespace ServicioVentas.Dominio.Constantes
{
    /// <summary>
    /// Estados del pago de una venta. Por ahora el sistema solo
    /// registra ventas con el pago completado; los estados
    /// pendientes/parciales (con saldo y abonos) quedan como módulo
    /// futuro y no se aceptan en el registro actual.
    /// </summary>
    public static class EstadoPago
    {
        public const string Pagado = "Pagado";
        public const string Anulado = "Anulado";
    }
}
