using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FERRETERIA__Joel.ServicioUsuarios.Seguridad
{
    /// <summary>
    /// Emite los JWT de la API. El token solo contiene datos de identidad
    /// mínimos (id, nombre, correo, rol, nombre de usuario y jti). Nunca
    /// incluye contraseñas, hashes ni tokens de activación.
    /// </summary>
    public class JwtEmisor
    {
        private readonly IConfiguration _configuration;
        private readonly RevocacionTokens _revocacion;

        public JwtEmisor(
            IConfiguration configuration,
            RevocacionTokens revocacion)
        {
            _configuration = configuration;
            _revocacion = revocacion;
        }

        public string Issuer =>
            _configuration["Jwt:Issuer"] ?? "FERRETERIA.Joel.ServicioUsuarios";

        public string Audience =>
            _configuration["Jwt:Audience"] ?? "FERRETERIA.Joel.Web";

        public int DuracionMinutos =>
            int.TryParse(_configuration["Jwt:DuracionMinutos"], out int minutos)
                ? minutos
                : 60;

        public (string Token, DateTime Expira) Emitir(
            int idEmpleado,
            string nombreCompleto,
            string correoElectronico,
            string rol,
            string? nombreUsuario)
        {
            DateTime expira = DateTime.UtcNow.AddMinutes(DuracionMinutos);

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, idEmpleado.ToString()),
                new Claim(ClaimTypes.Name, nombreCompleto),
                new Claim(ClaimTypes.Email, correoElectronico),
                new Claim(ClaimTypes.Role, rol),
                new Claim("gen", _revocacion.ObtenerGeneracion(idEmpleado).ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            if (!string.IsNullOrWhiteSpace(nombreUsuario))
            {
                claims.Add(new Claim("nombre_usuario", nombreUsuario));
            }

            SymmetricSecurityKey clave = new(
                Encoding.UTF8.GetBytes(RequerirClave()));

            JwtSecurityToken token = new(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expira,
                signingCredentials: new SigningCredentials(
                    clave,
                    SecurityAlgorithms.HmacSha256));

            return (
                new JwtSecurityTokenHandler().WriteToken(token),
                expira);
        }

        public string RequerirClave()
        {
            string? clave = _configuration["Jwt:Clave"];

            if (string.IsNullOrWhiteSpace(clave) || clave.Length < 32)
            {
                throw new InvalidOperationException(
                    "Falta 'Jwt:Clave' o es demasiado corta (mínimo 32 caracteres). "
                    + "Defina la variable de entorno Jwt__Clave (archivo .env).");
            }

            return clave;
        }
    }
}
