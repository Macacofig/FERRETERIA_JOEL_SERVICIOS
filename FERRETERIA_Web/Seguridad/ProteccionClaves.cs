using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FERRETERIA__Joel.Seguridad
{
    /// <summary>
    /// Carga el certificado con el que se cifran las claves de Data
    /// Protection.
    ///
    /// <para>
    /// Las claves de Data Protection se guardan en Redis para que todas las
    /// réplicas del monolito puedan descifrar las cookies de sesión y de
    /// antiforgery. Sin cifrado adicional, esas claves quedan almacenadas en
    /// claro dentro de Redis: con el certificado, quedan protegidas.
    /// </para>
    ///
    /// <para>
    /// El certificado debe ser <em>el mismo</em> en todas las réplicas. Por
    /// eso el punto de entrada del contenedor lo genera una sola vez en un
    /// volumen compartido por las cuatro réplicas.
    /// </para>
    /// </summary>
    public static class ProteccionClaves
    {
        /// <summary>
        /// Carga el certificado configurado en
        /// <c>DataProtection:CertificadoRuta</c>. Devuelve <c>null</c> si no
        /// hay ninguna ruta configurada, si el archivo no existe o si el
        /// certificado no se puede leer: en ese caso la aplicación arranca
        /// igual y queda registrado en el log.
        /// </summary>
        public static X509Certificate2? CargarCertificado(
            IConfiguration configuration)
        {
            string? ruta = configuration["DataProtection:CertificadoRuta"]
                ?? Environment.GetEnvironmentVariable(
                    "DataProtection__CertificadoRuta");

            if (string.IsNullOrWhiteSpace(ruta))
            {
                return null;
            }

            if (!File.Exists(ruta))
            {
                return null;
            }

            string contrasena = configuration["DataProtection:CertificadoContrasena"]
                ?? Environment.GetEnvironmentVariable(
                    "DataProtection__CertificadoContrasena")
                ?? string.Empty;

            try
            {
                // LoadPkcs12 es la API recomendada en .NET 9+: carga el
                // certificado sin dejar la contrasena expuesta en memoria y
                // sin la advertencia de obsolescencia del constructor.
                byte[] contenido = File.ReadAllBytes(ruta);

                return X509CertificateLoader.LoadPkcs12(
                    contenido,
                    contrasena,
                    X509KeyStorageFlags.Exportable
                        | X509KeyStorageFlags.MachineKeySet);
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
