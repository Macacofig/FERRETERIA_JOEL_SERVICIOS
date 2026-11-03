using System.Text.RegularExpressions;
using FERRETERIA__Joel.Dominio.ConfiguracionValidacion;

namespace FERRETERIA__Joel.Dominio.Validaciones
{
    /// <summary>
    /// Reglas comunes de credenciales compartidas por el registro de
    /// empleados y de clientes: nombres, teléfono, correo, contraseña y su
    /// confirmación. No conoce ni consulta la base de datos.
    /// </summary>
    public class CredencialesValidaciones
    {
        // 8 dígitos: celulares en Bolivia inician exclusivamente con 6 o 7
        private static readonly Regex FormatoTelefono =
            new(@"^[67][0-9]{7}$", RegexOptions.Compiled);

        private static readonly Regex FormatoCorreo =
            new(@"^(?=.{1,254}$)(?=.{6,64}@)[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)*\.[A-Za-z]{2,24}$", RegexOptions.Compiled);

        public bool EsNombreValido(string? nombre)
        {
            return ValidacionesTexto.EsNombreValido(
                nombre,
                ConfiguracionCredenciales.NombreMaxLength);
        }

        public bool EsApellidoValido(string? apellido)
        {
            return ValidacionesTexto.EsNombreValido(
                apellido,
                ConfiguracionCredenciales.ApellidoMaxLength);
        }

        public bool EsTelefonoValido(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
            {
                return false;
            }

            string limpio = telefono.Trim();

            if (!FormatoTelefono.IsMatch(limpio))
            {
                return false;
            }

            if (TodosDigitosIguales(limpio))
            {
                return false;
            }

            if (EsSecuenciaConcurrente(limpio))
            {
                return false;
            }

            return true;
        }

        private static bool TodosDigitosIguales(string valor)
        {
            char primerDigito = valor[0];
            for (int i = 1; i < valor.Length; i++)
            {
                if (valor[i] != primerDigito)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool EsSecuenciaConcurrente(string valor)
        {
            bool ascendente = true;
            bool descendente = true;

            for (int i = 0; i < valor.Length - 1; i++)
            {
                if (valor[i + 1] - valor[i] != 1)
                {
                    ascendente = false;
                }
                if (valor[i] - valor[i + 1] != 1)
                {
                    descendente = false;
                }
            }

            return ascendente || descendente;
        }

        public bool EsCorreoValido(string? correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                return false;
            }

            string limpio = correo.Trim();

            return limpio.Length <= ConfiguracionCredenciales.CorreoMaxLength
                && FormatoCorreo.IsMatch(limpio);
        }

        public bool EsContrasenaValida(string? contrasena)
        {
            if (string.IsNullOrWhiteSpace(contrasena))
            {
                return false;
            }

            if (contrasena.Length < ConfiguracionCredenciales.ContrasenaLongitudMinima
                || contrasena.Length > ConfiguracionCredenciales.ContrasenaLongitudMaxima)
            {
                return false;
            }

            if (contrasena.Count(char.IsUpper) < ConfiguracionCredenciales.ContrasenaMayusculasMinimas)
            {
                return false;
            }

            if (contrasena.Count(char.IsLower) < ConfiguracionCredenciales.ContrasenaMinusculasMinimas)
            {
                return false;
            }

            if (contrasena.Count(char.IsDigit) < ConfiguracionCredenciales.ContrasenaDigitosMinimos)
            {
                return false;
            }

            int simbolos = contrasena.Count(
                c => ConfiguracionCredenciales.SimbolosContrasena.Contains(c));

            return simbolos >= ConfiguracionCredenciales.ContrasenaSimbolosMinimos;
        }

        public bool EsConfirmacionValida(string? contrasena, string? confirmacion)
        {
            return !string.IsNullOrEmpty(contrasena)
                && contrasena == confirmacion;
        }
    }
}