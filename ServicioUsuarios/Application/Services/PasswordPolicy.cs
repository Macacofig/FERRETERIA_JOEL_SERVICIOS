using ServicioUsuarios.Application.Ports;

namespace ServicioUsuarios.Application.Services;

public sealed class PasswordPolicy : IPasswordPolicy
{
    public bool EsValida(string password) =>
        password.Length >= 8 &&
        password.Any(char.IsDigit) &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(c => !char.IsLetterOrDigit(c));

    public string ObtenerMensaje() =>
        "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial.";
}
