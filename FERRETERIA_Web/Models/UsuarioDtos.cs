namespace FERRETERIA_Web.Models;

public sealed record LoginRequest(string NombreUsuario, string Password);
public sealed record LoginResponse(string Token);

public sealed record UsuarioResponse(
    long Id,
    string NombreCompleto,
    string NombreUsuario,
    string Correo,
    string Rol,
    bool Activo,
    bool DebeCambiarPassword);

public sealed record CrearUsuarioRequest(string NombreCompleto, string Correo, string Rol);
public sealed record EditarUsuarioRequest(string NombreCompleto, string Correo, string Rol);
public sealed record ActivarCuentaRequest(string Password, string PasswordConfirmacion);
public sealed record CambiarPasswordRequest(
    string PasswordActual,
    string PasswordNueva,
    string PasswordConfirmacion);
