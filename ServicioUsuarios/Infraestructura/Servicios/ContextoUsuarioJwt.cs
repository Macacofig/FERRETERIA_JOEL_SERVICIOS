using System.Security.Claims;
using FERRETERIA__Joel.Dominio.Puertos;

namespace FERRETERIA__Joel.Infraestructura.Servicios
{
    /// <summary>
    /// Resuelve la identidad del empleado a partir del JWT validado. Es el
    /// adaptador del puerto <see cref="IContextoUsuario"/> dentro de la API.
    /// </summary>
    public class ContextoUsuarioJwt : IContextoUsuario
    {
        private readonly IHttpContextAccessor _accesor;

        public ContextoUsuarioJwt(IHttpContextAccessor accesor)
        {
            _accesor = accesor;
        }

        private ClaimsPrincipal? Usuario => _accesor.HttpContext?.User;

        public int IdEmpleado =>
            int.TryParse(
                Usuario?.FindFirstValue(ClaimTypes.NameIdentifier),
                out int id)
                ? id
                : 0;

        public string NombreCompleto =>
            Usuario?.FindFirstValue(ClaimTypes.Name)
            ?? Usuario?.Identity?.Name
            ?? string.Empty;

        public string Rol =>
            Usuario?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        public string? NombreUsuario =>
            Usuario?.FindFirstValue("nombre_usuario");
    }
}

