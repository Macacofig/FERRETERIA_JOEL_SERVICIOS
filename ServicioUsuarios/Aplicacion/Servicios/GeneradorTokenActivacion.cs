using System.Security.Cryptography;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Genera los tokens de activación de cuenta. Se usa un valor aleatorio
    /// criptográficamente seguro codificado en Base64Url, apto para viajar
    /// dentro de un enlace.
    /// </summary>
    public static class GeneradorTokenActivacion
    {
        private const int BytesToken = 32;

        public static string Generar()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(BytesToken);

            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
