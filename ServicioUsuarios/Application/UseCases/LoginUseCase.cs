using ServicioUsuarios.Application.Ports;
using ServicioUsuarios.Domain.Entities;

namespace ServicioUsuarios.Application.UseCases;

public sealed class LoginUseCase(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    public async Task<string?> EjecutarAsync(string nombreUsuario, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
            return null;

        Usuario? usuario = await usuarioRepository.BuscarPorNombreUsuarioAsync(nombreUsuario.Trim(), cancellationToken);
        if (usuario is null || !usuario.Activo || !passwordHasher.Verify(password, usuario.PasswordHash))
            return null;

        return tokenService.CrearToken(usuario);
    }
}
