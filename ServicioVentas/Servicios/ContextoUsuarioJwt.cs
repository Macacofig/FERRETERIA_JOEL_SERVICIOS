using System.Security.Claims;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Servicios
{
    /// <summary>
    /// Resuelve la identidad del empleado autenticado a partir de los
    /// claims del JWT token enviado por la UI en cada request.
    ///
    /// La UI pasa un token JWT en el header Authorization: Bearer <token>.
    /// Este adaptador lee los claims NameIdentifier (id de empleado),
    /// Name (nombre completo) y Role (rol del usuario).
    /// </summary>
    public class ContextoUsuarioJwt : IContextoUsuario
    {
        private readonly IHttpContextAccessor _accesor;

        public ContextoUsuarioJwt(IHttpContextAccessor accesor)
        {
            _accesor = accesor;
        }

        private ClaimsPrincipal? Usuario => _accesor.HttpContext?.User;

        public int IdEmpleado
        {
            get
            {
                string? valor = Usuario?.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(valor, out int id) ? id : 0;
            }
        }

        public string NombreCompleto =>
            Usuario?.FindFirstValue(ClaimTypes.Name)
            ?? Usuario?.Identity?.Name
            ?? string.Empty;

        public string Rol =>
            Usuario?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
