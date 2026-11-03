using System.Globalization;
using System.Text;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Genera el nombre de usuario a partir del nombre y el apellido con una
    /// convención determinista: primera letra del nombre + apellido completo,
    /// sin acentos ni caracteres especiales y en minúsculas (por ejemplo
    /// "María López" → "mlopez"). Si el valor ya existe se agrega un sufijo
    /// numérico ("mlopez2", "mlopez3", ...). La base de datos además tiene un
    /// índice único, de modo que dos solicitudes simultáneas no pueden dejar
    /// dos cuentas con el mismo usuario.
    /// </summary>
    public static class GeneradorNombreUsuario
    {
        public const int LongitudMaxima = 60;

        public static string Base(string? nombre, string? apellido)
        {
            string nombreLimpio = Limpiar(nombre);
            string apellidoLimpio = Limpiar(apellido);

            string inicial = nombreLimpio.Length > 0
                ? nombreLimpio[..1]
                : "u";

            string baseUsuario = inicial + apellidoLimpio;

            if (baseUsuario.Length == 0)
            {
                baseUsuario = "usuario";
            }

            return baseUsuario.Length > LongitudMaxima
                ? baseUsuario[..LongitudMaxima]
                : baseUsuario;
        }

        public static string ConSufijo(string baseUsuario, int sufijo)
        {
            string texto = sufijo <= 1
                ? baseUsuario
                : baseUsuario + sufijo.ToString(CultureInfo.InvariantCulture);

            return texto.Length > LongitudMaxima
                ? texto[..LongitudMaxima]
                : texto;
        }

        private static string Limpiar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            string descompuesto = valor.Trim()
                .Normalize(NormalizationForm.FormD);

            StringBuilder resultado = new();

            foreach (char caracter in descompuesto)
            {
                UnicodeCategory categoria =
                    CharUnicodeInfo.GetUnicodeCategory(caracter);

                if (categoria == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (caracter is >= 'a' and <= 'z'
                    or >= 'A' and <= 'Z'
                    or >= '0' and <= '9')
                {
                    resultado.Append(char.ToLowerInvariant(caracter));
                }
            }

            return resultado.ToString();
        }
    }
}
