using FERRETERIA__Joel.Dominio.Puertos;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FERRETERIA__Joel.Infraestructura.Servicios
{
    /// <summary>
    /// Enviador de correo por SMTP (MailKit). La configuraciÃ³n se toma de la
    /// secciÃ³n <c>Smtp</c> del archivo <c>.env</c>/appsettings:
    /// <c>Smtp:Host</c>, <c>Smtp:Puerto</c>, <c>Smtp:Usuario</c>,
    /// <c>Smtp:Clave</c>, <c>Smtp:Remitente</c> y <c>Smtp:UsarSsl</c>.
    ///
    /// Cuando no hay SMTP configurado (host vacÃ­o), el enlace de activaciÃ³n se
    /// escribe en el log para poder probar el flujo en desarrollo sin servidor
    /// de correo.
    /// </summary>
    public class SmtpEnviadorCorreo : IEnviadorCorreo
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEnviadorCorreo> _logger;

        public SmtpEnviadorCorreo(
            IConfiguration configuration,
            ILogger<SmtpEnviadorCorreo> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private string Host =>
            _configuration["Smtp:Host"]?.Trim() ?? string.Empty;

        private bool EstaConfigurado =>
            !string.IsNullOrWhiteSpace(Host);

        public async Task EnviarActivacionAsync(
            string destinatario,
            string nombreEmpleado,
            string enlaceActivacion)
        {
            if (!EstaConfigurado)
            {
                _logger.LogWarning(
                    "SMTP no configurado. Enlace de activaciÃ³n para {Correo}: {Enlace}",
                    destinatario,
                    enlaceActivacion);

                return;
            }

            MimeMessage mensaje = ConstruirMensaje(
                destinatario,
                nombreEmpleado,
                enlaceActivacion);

            using SmtpClient cliente = new();

            SecureSocketOptions seguridad =
                LeerBooleano("Smtp:UsarSsl", true)
                    ? SecureSocketOptions.StartTlsWhenAvailable
                    : SecureSocketOptions.None;

            await cliente.ConnectAsync(
                Host,
                LeerEntero("Smtp:Puerto", 587),
                seguridad);

            string? usuario = _configuration["Smtp:Usuario"];

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                await cliente.AuthenticateAsync(
                    usuario,
                    _configuration["Smtp:Clave"] ?? string.Empty);
            }

            await cliente.SendAsync(mensaje);

            await cliente.DisconnectAsync(true);

            _logger.LogInformation(
                "Correo de activaciÃ³n enviado a {Correo}.",
                destinatario);
        }

        private MimeMessage ConstruirMensaje(
            string destinatario,
            string nombreEmpleado,
            string enlaceActivacion)
        {
            string remitente =
                _configuration["Smtp:Remitente"]?.Trim()
                ?? _configuration["Smtp:Usuario"]?.Trim()
                ?? "no-reply@ferreteriajoel.com";

            string remitenteNombre =
                _configuration["Smtp:RemitenteNombre"]?.Trim()
                ?? "FerreterÃ­a Joel";

            MimeMessage mensaje = new();
            mensaje.From.Add(new MailboxAddress(remitenteNombre, remitente));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = "Activa tu cuenta Â· FerreterÃ­a Joel";

            string saludo = string.IsNullOrWhiteSpace(nombreEmpleado)
                ? "Hola"
                : $"Hola {nombreEmpleado}";

            mensaje.Body = new BodyBuilder
            {
                HtmlBody = $@"<p>{saludo},</p>
<p>Se creÃ³ una cuenta para ti en el sistema de FerreterÃ­a Joel.</p>
<p>Para definir tu contraseÃ±a, abre el siguiente enlace (vÃ¡lido por 10 minutos):</p>
<p><a href=""{enlaceActivacion}"">Activar mi cuenta</a></p>
<p>Si no reconoces este registro, ignora este correo.</p>",
                TextBody = $@"{saludo},

Se creÃ³ una cuenta para ti en el sistema de FerreterÃ­a Joel.

Abre este enlace para definir tu contraseÃ±a (vÃ¡lido por 10 minutos):
{enlaceActivacion}

Si no reconoces este registro, ignora este correo."
            }.ToMessageBody();

            return mensaje;
        }

        private int LeerEntero(string clave, int valorPorDefecto)
        {
            return int.TryParse(_configuration[clave], out int valor)
                ? valor
                : valorPorDefecto;
        }

        private bool LeerBooleano(string clave, bool valorPorDefecto)
        {
            return bool.TryParse(_configuration[clave], out bool valor)
                ? valor
                : valorPorDefecto;
        }
    }
}

