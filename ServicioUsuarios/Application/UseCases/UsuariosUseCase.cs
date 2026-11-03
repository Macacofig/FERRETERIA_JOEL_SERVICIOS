using System.Security.Cryptography;
using ServicioUsuarios.Application.Ports;
using ServicioUsuarios.Domain.Entities;

namespace ServicioUsuarios.Application.UseCases;

public sealed class UsuariosUseCase(
    IUsuarioRepository repository,
    IUsernameGenerator usernameGenerator,
    IPasswordHasher passwordHasher,
    IPasswordPolicy passwordPolicy,
    IEmailSender emailSender,
    string activationUrlBase)
{
    public Task<List<Usuario>> ListarAsync(CancellationToken cancellationToken = default) =>
        repository.ListarAsync(false, cancellationToken);

    public Task<Usuario?> ObtenerAsync(long id, CancellationToken cancellationToken = default) =>
        repository.ObtenerPorIdAsync(id, cancellationToken);

    public async Task<(bool Exito, string Mensaje, Usuario? Usuario)> CrearAsync(
        string nombreCompleto, string correo, string rol, string creadoPor, CancellationToken cancellationToken = default)
    {
        if (rol != "Administrador" && rol != "EmpleadoVenta")
            return (false, "Rol inválido.", null);

        string nombreUsuario = await usernameGenerator.GenerarAsync(nombreCompleto, cancellationToken);
        string passwordTemporal = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var usuario = new Usuario
        {
            NombreCompleto = nombreCompleto.Trim(),
            NombreUsuario = nombreUsuario,
            Correo = correo.Trim(),
            PasswordHash = passwordHasher.Hash(passwordTemporal),
            Rol = rol,
            Activo = false,
            DebeCambiarPassword = true,
            CreadoPor = creadoPor,
            FechaCreacion = DateTime.UtcNow
        };

        long id = await repository.CrearAsync(usuario, cancellationToken);

        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
        string tokenPlano = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        string tokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(tokenPlano))).ToLowerInvariant();
        await repository.GuardarTokenActivacionAsync(id, tokenHash, DateTime.UtcNow.AddHours(24), creadoPor, cancellationToken);

        Usuario? creado = await repository.ObtenerPorIdAsync(id, cancellationToken);
        if (creado is null)
        {
            creado = new Usuario
            {
                Id = id,
                NombreCompleto = usuario.NombreCompleto,
                NombreUsuario = usuario.NombreUsuario,
                Correo = usuario.Correo,
                PasswordHash = string.Empty,
                Rol = usuario.Rol,
                Activo = false,
                DebeCambiarPassword = true,
                CreadoPor = creadoPor,
                FechaCreacion = DateTime.UtcNow
            };
        }
        string url = $"{activationUrlBase.TrimEnd('/')}?token={Uri.EscapeDataString(tokenPlano)}";
        await emailSender.EnviarActivacionAsync(correo.Trim(), nombreCompleto.Trim(), nombreUsuario, url, cancellationToken);
        return (true, "Usuario creado; activación enviada por correo.", creado);
    }

    public Task<bool> EditarAsync(long id, string nombreCompleto, string correo, string rol, string modificadoPor, CancellationToken cancellationToken = default) =>
        repository.EditarAsync(id, nombreCompleto, correo, rol, modificadoPor, cancellationToken);

    public Task<bool> EliminarAsync(long id, string dadoDeBajaPor, CancellationToken cancellationToken = default) =>
        repository.BajaLogicaAsync(id, dadoDeBajaPor, cancellationToken);

    public async Task<(bool Exito, string Mensaje)> ActivarAsync(string tokenPlano, string password, string passwordConfirmacion, CancellationToken cancellationToken = default)
    {
        if (!passwordPolicy.EsValida(password))
            return (false, passwordPolicy.ObtenerMensaje());
        if (!password.Equals(passwordConfirmacion, StringComparison.Ordinal))
            return (false, "Las contraseñas no coinciden.");

        string tokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(tokenPlano))).ToLowerInvariant();
        long? usuarioId = await repository.BuscarUsuarioPorTokenAsync(tokenHash, cancellationToken);
        if (usuarioId is null)
            return (false, "El token de activación no es válido o venció.");

        Usuario? usuarioActivado = await repository.ObtenerPorIdAsync(usuarioId.Value, cancellationToken);
        string actorActivacion = usuarioActivado is null
            ? $"usuario:{usuarioId.Value}"
            : $"usuario:{usuarioActivado.NombreUsuario}";
        bool activado = await repository.ActivarAsync(
            usuarioId.Value,
            tokenHash,
            passwordHasher.Hash(password),
            actorActivacion,
            cancellationToken);
        return activado
            ? (true, "Cuenta activada correctamente.")
            : (false, "No fue posible activar la cuenta.");
    }

    public async Task<(bool Exito, string Mensaje)> CambiarPasswordAsync(
        long usuarioId,
        string passwordActual,
        string passwordNueva,
        string passwordConfirmacion,
        string modificadoPor,
        CancellationToken cancellationToken = default)
    {
        Usuario? usuario = await repository.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null || !passwordHasher.Verify(passwordActual, usuario.PasswordHash))
            return (false, "La contraseña actual no es válida.");

        if (!passwordNueva.Equals(passwordConfirmacion, StringComparison.Ordinal))
            return (false, "La contraseña nueva y su confirmación no coinciden.");

        if (passwordHasher.Verify(passwordNueva, usuario.PasswordHash))
            return (false, "La contraseña nueva debe ser distinta de la contraseña actual.");

        if (!passwordPolicy.EsValida(passwordNueva))
            return (false, passwordPolicy.ObtenerMensaje());

        bool cambiado = await repository.CambiarPasswordAsync(
            usuarioId,
            passwordHasher.Hash(passwordNueva),
            modificadoPor,
            cancellationToken);

        return cambiado
            ? (true, "Contraseña cambiada correctamente.")
            : (false, "No fue posible cambiar la contraseña.");
    }
}
