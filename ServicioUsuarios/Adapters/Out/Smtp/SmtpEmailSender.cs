using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using ServicioUsuarios.Application.Ports;

namespace ServicioUsuarios.Adapters.Out.Smtp;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task EnviarActivacionAsync(string destinatario, string nombreCompleto, string nombreUsuario, string urlActivacion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.From))
            throw new InvalidOperationException("La configuración SMTP está incompleta.");

        using var mensaje = new MailMessage(_options.From, destinatario)
        {
            Subject = "Activación de cuenta - Ferretería Joel",
            Body = $"Hola {nombreCompleto},\n\nTu usuario es: {nombreUsuario}\nActiva tu cuenta: {urlActivacion}\n\nEl enlace vence en 24 horas.",
            IsBodyHtml = false
        };
        using var smtp = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.User, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await smtp.SendMailAsync(mensaje, cancellationToken);
    }
}
