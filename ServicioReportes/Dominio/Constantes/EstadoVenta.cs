namespace ServicioReportes.Dominio.Constantes
{
    /// <summary>
    /// Estados de venta admitidos por el reporte. Se usan como parámetro
    /// validado del filtro y como valor de presentación en los documentos.
    /// </summary>
    public static class EstadoVenta
    {
        public const string Registrada = "Registrada";
        public const string Anulada = "Anulada";

        public static string? Normalizar(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
            {
                return null;
            }

            string valor = estado.Trim();

            if (string.Equals(valor, Registrada, StringComparison.OrdinalIgnoreCase))
            {
                return Registrada;
            }

            if (string.Equals(valor, Anulada, StringComparison.OrdinalIgnoreCase))
            {
                return Anulada;
            }

            if (string.Equals(valor, "Todas", StringComparison.OrdinalIgnoreCase)
                || string.Equals(valor, "Todos", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            throw new ArgumentException(
                "El estado de venta debe ser 'Registrada', 'Anulada' o 'Todas'.",
                nameof(estado));
        }

        public static string Etiqueta(string? estado)
        {
            return estado ?? "Todas";
        }
    }
}
