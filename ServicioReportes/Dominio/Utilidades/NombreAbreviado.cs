using System.Globalization;

namespace ServicioReportes.Dominio.Utilidades
{
    /// <summary>
    /// Abrevia el nombre del responsable tal como exige la rúbrica:
    /// "María López" se muestra como "M. López".
    /// </summary>
    public static class NombreAbreviado
    {
        public static string De(string? nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                return "Usuario del sistema";
            }

            string[] palabras = nombreCompleto
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (palabras.Length == 1)
            {
                return Capitalizar(palabras[0]);
            }

            string primera = palabras[0];
            string ultima = palabras[^1];

            if (!char.IsLetter(primera[0]) || !char.IsLetter(ultima[0]))
            {
                return string.Join(' ', palabras);
            }

            return $"{char.ToUpperInvariant(primera[0])}. {Capitalizar(ultima)}";
        }

        private static string Capitalizar(string palabra)
        {
            return char.ToUpperInvariant(palabra[0])
                + palabra[1..].ToLower(CultureInfo.CurrentCulture);
        }
    }
}
