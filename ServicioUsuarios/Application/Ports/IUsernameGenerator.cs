namespace ServicioUsuarios.Application.Ports;

public interface IUsernameGenerator
{
    Task<string> GenerarAsync(string nombreCompleto, CancellationToken cancellationToken = default);
}
