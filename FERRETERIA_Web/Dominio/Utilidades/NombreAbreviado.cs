using System.Globalization;

namespace FERRETERIA__Joel.Dominio.Utilidades
{
    /// <summary>
    /// Abrevia un nombre completo para mostrarlo en documentos donde no debe
    /// aparecer completo: inicial del nombre, un punto y el último apellido.
    /// Ejemplo: "María López" produce "M.López".
    /// </summary>
    public static class NombreAbreviado
    {
        public static string De(string? nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                return string.Empty;
            }

            string[] palabras = nombreCompleto
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (palabras.Length == 1)
            {
                return Capitalizar(palabras[0]);
            }

            string primera = palabras[0];
            string ultima = palabras[^1];

            // Textos como "Empleado #5" no son nombres de persona: se dejan tal cual.
            if (!char.IsLetter(primera[0]) || !char.IsLetter(ultima[0]))
            {
                return string.Join(' ', palabras);
            }

            return $"{char.ToUpperInvariant(primera[0])}.{Capitalizar(ultima)}";
        }

        private static string Capitalizar(string palabra)
        {
            return char.ToUpperInvariant(palabra[0])
                + palabra[1..].ToLower(CultureInfo.CurrentCulture);
        }
    }
}
