using System.Text.RegularExpressions;

namespace ServicioVentas.Aplicacion.UseCases.Clientes
{
    /// <summary>
    /// Normalización común de los datos de cliente usados por los casos de
    /// uso de alta y búsqueda.
    /// </summary>
    internal static class ClienteNormalizador
    {
        // Quita espacios sobrantes: "  Ana   María " -> "Ana María".
        public static string Texto(string? texto)
        {
            return Regex.Replace(
                texto?.Trim() ?? string.Empty,
                @"\s+",
                " ");
        }

        // Quita espacios y pasa a mayúsculas: "1234567 - 1a" -> "1234567-1A".
        public static string Ci(string? ci)
        {
            return Regex.Replace(
                ci ?? string.Empty,
                @"\s",
                string.Empty).ToUpperInvariant();
        }
    }
}
