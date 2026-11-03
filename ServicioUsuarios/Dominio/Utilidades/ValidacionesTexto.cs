using System.Text.RegularExpressions;

namespace FERRETERIA__Joel.Dominio.Utilidades
{
    /// <summary>
    /// Reglas comunes de texto para todos los mÃ³dulos.
    /// Los campos de nombre solo admiten letras (con acentos y Ã‘),
    /// espacios y la puntuaciÃ³n habitual de nombres
    /// (., ' - &amp; ( )). No se permiten nÃºmeros
    /// ni caracteres especiales como * @ # % etc.
    /// La descripciÃ³n queda fuera de esta regla.
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

