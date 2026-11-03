using System.Text.RegularExpressions;

namespace FERRETERIA__Joel.Dominio.Validaciones
{
    /// <summary>
    /// Reglas comunes de texto para todos los módulos.
    /// Los campos de nombre solo admiten letras (con acentos y Ñ),
    /// espacios y la puntuación habitual de nombres
    /// (., ' - &amp; ( )). No se permiten números
    /// ni caracteres especiales como * @ # % etc.
    /// La descripción queda fuera de esta regla.
    /// </summary>
    public static class ValidacionesTexto
    {
        private static readonly Regex FormatoNombre =
            new(
                @"^\p{L}[\p{L}\s.,'\-&()]*$",
                RegexOptions.Compiled);

        public static bool EsNombreValido(
            string? texto,
            int longitudMaxima)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            string limpio = texto.Trim();

            if (limpio.Length > longitudMaxima)
            {
                return false;
            }

            return FormatoNombre.IsMatch(limpio);
        }
    }
}
