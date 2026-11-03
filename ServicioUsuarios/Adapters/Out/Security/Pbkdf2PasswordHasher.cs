using System.Security.Cryptography;
using ServicioUsuarios.Application.Ports;

namespace ServicioUsuarios.Adapters.Out.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iteraciones = 100_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        byte[] salt = RandomNumberGenerator.GetBytes(TamanoSal);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
        return $"PBKDF2-SHA256${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        string[] partes = hash.Split('$');
        if (partes.Length != 4 || !partes[0].Equals("PBKDF2-SHA256", StringComparison.Ordinal))
            return false;

        if (!int.TryParse(partes[1], out int iteraciones) || iteraciones < 1)
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(partes[2]);
            byte[] hashEsperado = Convert.FromBase64String(partes[3]);
            byte[] hashActual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iteraciones, HashAlgorithmName.SHA256, hashEsperado.Length);
            return CryptographicOperations.FixedTimeEquals(hashEsperado, hashActual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
