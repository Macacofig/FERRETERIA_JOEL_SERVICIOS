using FERRETERIA_Web.Models;

namespace FERRETERIA_Web.Services;

public interface IServicioUsuariosClient
{
    Task<(bool Exitoso, string Token, string Error)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioResponse>> ListarAsync(CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> ObtenerAsync(long id, CancellationToken cancellationToken = default);
    Task<(bool Exitoso, UsuarioResponse? Usuario, string Error)> CrearAsync(CrearUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<(bool Exitoso, string Error)> EditarAsync(long id, EditarUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<(bool Exitoso, string Error)> DarDeBajaAsync(long id, CancellationToken cancellationToken = default);
    Task<(bool Exitoso, string Error)> ActivarAsync(string token, ActivarCuentaRequest request, CancellationToken cancellationToken = default);
    Task<(bool Exitoso, string Error)> CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken cancellationToken = default);
}
