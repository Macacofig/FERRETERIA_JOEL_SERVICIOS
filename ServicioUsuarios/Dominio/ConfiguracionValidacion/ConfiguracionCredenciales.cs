namespace FERRETERIA__Joel.Dominio.ConfiguracionValidacion
{
    public static class ConfiguracionCredenciales
    {
        public const int NombreMaxLength = 100;
        public const int ApellidoMaxLength = 100;
        public const int TelefonoLongitud = 8;
        public const int CorreoMaxLength = 150;
        public const int ContrasenaLongitudMinima = 8;
        public const int ContrasenaLongitudMaxima = 64;
        public const int ContrasenaMayusculasMinimas = 1;
        public const int ContrasenaMinusculasMinimas = 1;
        public const int ContrasenaDigitosMinimos = 1;
        public const int ContrasenaSimbolosMinimos = 1;
        public const string SimbolosContrasena =
            "!@#$%^&*()-_=+[]{};:,.?/";
    }
}
