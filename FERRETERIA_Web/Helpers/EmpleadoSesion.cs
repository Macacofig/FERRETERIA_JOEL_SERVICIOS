using System.Security.Claims;

namespace FERRETERIA__Joel.Helpers
{
    // Acceso al empleado autenticado a partir de la cookie de sesión.
    public static class EmpleadoSesion
    {
        public static int ObtenerIdEmpleado(ClaimsPrincipal usuario)
        {
            string? valor =
                usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(valor, out int id) && id > 0)
            {
                return id;
            }

            throw new InvalidOperationException(
                "No se encontró el empleado autenticado en la sesión.");
        }
    }
}
