using System.Security.Cryptography;
using System.Text;

namespace FERRETERIA__Joel.Helpers
{
    public static class UrlProtector
    {
        private static byte[] _clave = Array.Empty<byte>();

        public static void Inicializar(string claveTexto)
        {
            _clave = SHA256.HashData(
                Encoding.UTF8.GetBytes(claveTexto));
        }

        public static string Cifrar(string texto)
        {
            byte[] textoBytes = Encoding.UTF8.GetBytes(texto);
            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] cifrado = new byte[textoBytes.Length];
            byte[] tag = new byte[16];

            using (var aes = new AesGcm(_clave, 16))
            {
                aes.Encrypt(nonce, textoBytes, cifrado, tag);
            }

            byte[] resultado = new byte[12 + 16 + cifrado.Length];
            nonce.CopyTo(resultado, 0);
            tag.CopyTo(resultado, 12);
            cifrado.CopyTo(resultado, 12 + 16);

            return Convert.ToBase64String(resultado)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        public static string? Descifrar(string token)
        {
            try
            {
                string base64 = token
                    .Replace('-', '+')
                    .Replace('_', '/');

                base64 = base64.PadRight(
                    base64.Length + (4 - base64.Length % 4) % 4,
                    '=');

                byte[] datos = Convert.FromBase64String(base64);

                if (datos.Length < 28)
                {
                    return null;
                }

                byte[] nonce = datos.Take(12).ToArray();
                byte[] tag = datos.Skip(12).Take(16).ToArray();
                byte[] cifrado = datos.Skip(28).ToArray();
                byte[] textoBytes = new byte[cifrado.Length];

                using (var aes = new AesGcm(_clave, 16))
                {
                    aes.Decrypt(nonce, cifrado, tag, textoBytes);
                }

                return Encoding.UTF8.GetString(textoBytes);
            }
            catch
            {
                return null;
            }
        }
    }
}