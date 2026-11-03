using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ServicioUsuarios.Application.Ports;

namespace ServicioUsuarios.Application.Services;

public sealed partial class UsernameGenerator(IUsuarioRepository repository) : IUsernameGenerator
{
    public async Task<string> GenerarAsync(string nombreCompleto, CancellationToken cancellationToken = default)
    {
        string[] partes = nombreCompleto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0)
            throw new ArgumentException("El nombre completo es requerido.");

        string inicialNombre = Limpiar(partes[0])[..1].ToLowerInvariant();
        string primerApellido = partes.Length > 1 ? Limpiar(partes[^1]).ToLowerInvariant() : Limpiar(partes[0]).ToLowerInvariant();
        string baseNombre = $"{inicialNombre}{primerApellido}";

        if (!await repository.ExisteNombreUsuarioAsync(baseNombre, cancellationToken))
            return baseNombre;

        for (int sufijo = 2; sufijo < 10000; sufijo++)
        {
            string candidato = $"{baseNombre}{sufijo}";
            if (!await repository.ExisteNombreUsuarioAsync(candidato, cancellationToken))
                return candidato;
        }

        throw new InvalidOperationException("No fue posible generar un nombre de usuario único.");
    }

    private static string Limpiar(string valor)
    {
        string normalized = valor.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            UnicodeCategory category = char.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return Regex().Replace(builder.ToString().Normalize(NormalizationForm.FormC), string.Empty).ToLowerInvariant();
    }

    [GeneratedRegex("[^a-z0-9]")]
    private static partial Regex Regex();
}
