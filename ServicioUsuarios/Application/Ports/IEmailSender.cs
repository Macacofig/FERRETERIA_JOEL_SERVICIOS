namespace ServicioUsuarios.Application.Ports;

public interface IEmailSender
{
    Task EnviarActivacionAsync(string destinatario, string nombreCompleto, string nombreUsuario, string urlActivacion, CancellationToken cancellationToken = default);
}
