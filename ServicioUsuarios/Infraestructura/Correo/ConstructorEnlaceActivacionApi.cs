using FERRETERIA__Joel.Aplicacion.Servicios;

namespace FERRETERIA__Joel.ServicioUsuarios.Servicios
{
    /// <summary>
    /// Construye el enlace absoluto de activación apuntando al front Razor
    /// (<c>App:UrlWeb</c>). El token viaja cifrado con AES-GCM.
    /// </summary>
    public class ConstructorEnlaceActivacionApi : IConstructorEnlaceActivacion
    {
        private readonly IConfiguration _configuration;

        public ConstructorEnlaceActivacionApi(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool EsEnlaceDeDesarrollo =>
            string.IsNullOrWhiteSpace(_configuration["Smtp:Host"]);

        public string Construir(string token)
        {
            string baseUrl =
                _configuration["App:UrlWeb"]?.TrimEnd('/')
                ?? "http://localhost:5080";

            string tokenProtegido =
                Seguridad.UrlProtectorApi.Cifrar(token);

            return $"{baseUrl}/ActivarCuenta?token={Uri.EscapeDataString(tokenProtegido)}";
        }
    }
}
