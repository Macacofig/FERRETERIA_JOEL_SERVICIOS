using System.Security.Cryptography;
using System.Text;
using FERRETERIA__Joel.Dominio.Puertos;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;

namespace FERRETERIA__Joel.Infraestructura.Seguridad
{
    // Hasheador de contraseñas con Argon2id (Isopoh): salt aleatorio por usuario y hash PHC autocontenido. (extra: Verificar en tiempo constante)
    public class Argon2HasheadorContrasena : IHasheadorContrasena
    {
        public string Hashear(string contrasena)
        {
            if (string.IsNullOrEmpty(contrasena))
            {
                throw new ArgumentException(
                    "La contraseña no puede estar vacía.",
                    nameof(contrasena));
            }

            byte[] salt = RandomNumberGenerator.GetBytes(
                ConfiguracionArgon2.SaltLength);

            Argon2Config config = new()
            {
                Type = Argon2Type.HybridAddressing,
                Version = Argon2Version.Nineteen,
                TimeCost = ConfiguracionArgon2.TimeCost,
                MemoryCost = ConfiguracionArgon2.MemoryCost,
                Lanes = ConfiguracionArgon2.Lanes,
                Threads = ConfiguracionArgon2.Threads,
                Password = Encoding.UTF8.GetBytes(contrasena),
                Salt = salt,
                HashLength = ConfiguracionArgon2.HashLength
            };

            Argon2 argon2 = new(config);

            using SecureArray<byte> hash = argon2.Hash();

            return config.EncodeString(hash.Buffer);
        }

        public bool Verificar(string contrasena, string hash)
        {
            if (string.IsNullOrEmpty(contrasena)
                || string.IsNullOrEmpty(hash))
            {
                return false;
            }

            try
            {
                return Argon2.Verify(hash, contrasena);
            }
            catch
            {
                // Hash almacenado con formato inválido o no generado con
                // Argon2: se trata como credencial no válida.
                return false;
            }
        }
    }
}
