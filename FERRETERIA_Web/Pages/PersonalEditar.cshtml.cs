using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Edición de los datos personales de una cuenta. No permite cambiar la
    /// contraseña ni el estado: esos flujos tienen su propia pantalla y su
    /// propia regla de negocio en ServicioUsuarios.
    /// </summary>
    [Authorize(Roles = RolesSistema.Administrador)]
    public class PersonalEditarModel : PageModel
    {
        private readonly UsuariosApiCliente _api;
        private readonly ILogger<PersonalEditarModel> _logger;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public ActualizarUsuarioApi Solicitud { get; set; } = new();

        public string? NombreUsuario { get; set; }

        public List<string> Errores { get; set; } = new();

        public Dictionary<string, string> ErroresCampo { get; set; } = new();

        public PersonalEditarModel(
            UsuariosApiCliente api,
            ILogger<PersonalEditarModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ViewData["ActiveMenu"] = "Registro";

            if (id <= 0)
            {
                return RedirectToPage("/Personal");
            }

            try
            {
                RespuestaApi<UsuarioApi> respuesta =
                    await _api.ObtenerAsync(ObtenerToken(), id);

                if (!respuesta.Exitoso || respuesta.Datos is null)
                {
                    TempData["MensajeError"] = string.IsNullOrWhiteSpace(
                        respuesta.Mensaje)
                        ? "El empleado no existe."
                        : respuesta.Mensaje;
                    return RedirectToPage("/Personal");
                }

                Id = respuesta.Datos.IdEmpleado;
                NombreUsuario = respuesta.Datos.NombreUsuario;
                Solicitud = new ActualizarUsuarioApi
                {
                    Nombre = respuesta.Datos.Nombre,
                    Apellido = respuesta.Datos.Apellido,
                    Telefono = respuesta.Datos.Telefono,
                    CorreoElectronico =
                        respuesta.Datos.CorreoElectronico ?? string.Empty,
                    Rol = respuesta.Datos.Rol
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el empleado {Id}.", id);
                TempData["MensajeError"] =
                    "No se pudo cargar el empleado.";
                return RedirectToPage("/Personal");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["ActiveMenu"] = "Registro";

            if (Id <= 0)
            {
                return RedirectToPage("/Personal");
            }

            try
            {
                RespuestaApi<UsuarioApi> respuesta =
                    await _api.ActualizarAsync(
                        ObtenerToken(),
                        Id,
                        Solicitud);

                if (!respuesta.Exitoso)
                {
                    Errores = respuesta.Errores.Any()
                        ? respuesta.Errores
                        : new List<string>
                        {
                            string.IsNullOrWhiteSpace(respuesta.Mensaje)
                                ? "No se pudo actualizar el empleado."
                                : respuesta.Mensaje
                        };

                    ErroresCampo = respuesta.ErroresCampo;
                    NombreUsuario = respuesta.Datos?.NombreUsuario;
                    return Page();
                }

                TempData["Mensaje"] = "Empleado actualizado.";
                return RedirectToPage("/Personal");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el empleado {Id}.", Id);
                Errores.Add(
                    "No se pudo actualizar el empleado. Inténtalo nuevamente.");
                return Page();
            }
        }

        private string? ObtenerToken()
        {
            return HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);
        }
    }
}
