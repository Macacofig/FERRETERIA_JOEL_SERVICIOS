using System.Text.RegularExpressions;

namespace ServicioVentas.Aplicacion.UseCases.Clientes
{
    /// <summary>
    /// Valida los datos del cliente que se registra desde la pantalla de
    /// venta (CI, nombres y apellidos) y arma un mensaje por campo.
    /// </summary>
    public class ClienteValidador
    {
        public const string CampoNombre = "Nombre";
        public const string CampoApellido = "Apellido";
        public const string CampoCi = "Ci";

        private static readonly Regex FormatoCi = new(
            "^[0-9]{6,8}(-[0-9][A-Z])?$",
            RegexOptions.Compiled);

        private const string MensajeCi =
            "El CI debe tener de 6 a 8 dígitos y, si tiene complemento, un guion con un dígito y una letra (ej: 1234567-1A).";
        private static readonly string MensajeNombre =
            "Los nombres solo admiten letras y espacios (máximo 100 caracteres).";
        private static readonly string MensajeApellido =
            "Los apellidos solo admiten letras y espacios (máximo 100 caracteres).";

        public Dictionary<string, string> Validar(
            string ci,
            string nombre,
            string apellido)
        {
            Dictionary<string, string> errores = new();

            if (string.IsNullOrWhiteSpace(ci))
            {
                errores[CampoCi] = "El CI del cliente es obligatorio.";
            }
            else if (!FormatoCi.IsMatch(ci))
            {
                errores[CampoCi] = MensajeCi;
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                errores[CampoNombre] = "Los nombres del cliente son obligatorios.";
            }
            else if (!EsNombreValido(nombre))
            {
                errores[CampoNombre] = MensajeNombre;
            }

            if (string.IsNullOrWhiteSpace(apellido))
            {
                errores[CampoApellido] = "Los apellidos del cliente son obligatorios.";
            }
            else if (!EsApellidoValido(apellido))
            {
                errores[CampoApellido] = MensajeApellido;
            }

            return errores;
        }

        private static bool EsNombreValido(string nombre)
        {
            return !string.IsNullOrWhiteSpace(nombre)
                && nombre.Trim().Length <= 100
                && Regex.IsMatch(nombre.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$");
        }

        private static bool EsApellidoValido(string apellido)
        {
            return !string.IsNullOrWhiteSpace(apellido)
                && apellido.Trim().Length <= 100
                && Regex.IsMatch(apellido.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$");
        }
    }
}
