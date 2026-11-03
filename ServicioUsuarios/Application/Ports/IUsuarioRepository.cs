using ServicioUsuarios.Domain.Entities;

namespace ServicioUsuarios.Application.Ports;

public interface IUsuarioRepository
{
    Task<Usuario?> BuscarPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default);
    Task<List<Usuario>> ListarAsync(bool incluirInactivos, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default);
    Task<long> CrearAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<bool> EditarAsync(long id, string nombreCompleto, string correo, string rol, string modificadoPor, CancellationToken cancellationToken = default);
    Task<bool> BajaLogicaAsync(long id, string dadoDeBajaPor, CancellationToken cancellationToken = default);
    Task<bool> ActivarAsync(long usuarioId, string tokenHash, string passwordHash, string modificadoPor, CancellationToken cancellationToken = default);
    Task<bool> CambiarPasswordAsync(long usuarioId, string passwordHash, string modificadoPor, CancellationToken cancellationToken = default);
    Task<long?> BuscarUsuarioPorTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task GuardarTokenActivacionAsync(long usuarioId, string tokenHash, DateTime vencimiento, string creadoPor, CancellationToken cancellationToken = default);
}
