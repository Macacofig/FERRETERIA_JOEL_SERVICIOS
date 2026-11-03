using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Alta de personal exclusiva del administrador. El front solo presenta el
    /// formulario: la validación, la generación del nombre de usuario, el alta
    /// pendiente y el envío del enlace de activación ocurren en
    /// ServicioUsuarios.
    /// </summary>
    [Authorize(Roles = RolesSistema.Administrador)]
    public class RegistroModel : PageModel
    {
        private readonly UsuariosApiCliente _api;
        private readonly ILogger<RegistroModel> _logger;

        [BindProperty]
        public RegistroSolicitud Solicitud { get; set; } = new();

        public List<string> Errores { get; set; } = new();
        public Dictionary<string, string> ErroresCampo { get; set; } = new();

        public string? EnlaceActivacion { get; set; }

        /// <summary>Nombre de usuario generado por la API en el alta.</summary>
        public string? NombreUsuarioGenerado { get; set; }

        public string? CorreoDuplicado { get; set; }

        public bool CorreoDuplicadoPendiente { get; set; }

        public RegistroModel(
            UsuariosApiCliente api,
            ILogger<RegistroModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public void OnGet()
        {
            ViewData["ActiveMenu"] = "Registro";
        }

        public async Task<IActionResult> OnPostRegistrarAsync()
        {
            ViewData["ActiveMenu"] = "Registro";
            NormalizarDatos();

            string? token = HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);

            try
            {
                RespuestaApi<CrearUsuarioApiRespuesta> respuesta =
                    await _api.CrearAsync(token, new CrearUsuarioApi
                    {
                        Nombre = Solicitud.Nombre,
                        Apellido = Solicitud.Apellido,
                        Telefono = Solicitud.Telefono,
                        CorreoElectronico = Solicitud.CorreoElectronico,
                        Rol = Solicitud.Rol
                    });

                if (!respuesta.Exitoso || respuesta.Datos is null)
                {
                    Errores = respuesta.Errores.Any()
                        ? respuesta.Errores
                        : new List<string>
                        {
                            string.IsNullOrWhiteSpace(respuesta.Mensaje)
                                ? "No se pudo registrar la cuenta."
                                : respuesta.Mensaje
                        };

                    ErroresCampo = respuesta.ErroresCampo;

                    if (!Errores.Any() && ErroresCampo.Any())
                    {
                        Errores.Add(
                            "Revise los campos señalados e inténtelo nuevamente.");
                    }

                    await MarcarCorreoDuplicadoAsync(token);

                    return Page();
                }

                TempData["Mensaje"] =
                    "Cuenta creada con éxito. Se envió el enlace de activación al correo.";

                EnlaceActivacion = respuesta.Datos.EnlaceActivacion;
                NombreUsuarioGenerado = respuesta.Datos.NombreUsuario;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar la cuenta vía API.");
                Errores.Add(
                    "No se pudo completar el registro. Inténtalo nuevamente.");
                return Page();
            }
        }

        /// <summary>
        /// Reenvía el enlace de activación a una cuenta pendiente localizada
        /// por correo (caso de alta con correo ya existente).
        /// </summary>
        public async Task<IActionResult> OnPostReenviarCorreoAsync(string correo)
        {
            ViewData["ActiveMenu"] = "Registro";

            string? token = HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);

            try
            {
                RespuestaApi<List<UsuarioApi>> pendientes =
                    await _api.PendientesAsync(token);

                UsuarioApi? usuario = pendientes.Datos?
                    .FirstOrDefault(u => string.Equals(
                        u.CorreoElectronico,
                        correo?.Trim(),
                        StringComparison.OrdinalIgnoreCase));

                if (usuario is null)
                {
                    Errores.Add(
                        "No existe una cuenta pendiente con ese correo.");
                    return Page();
                }

                RespuestaApi<object> resultado =
                    await _api.ReenviarActivacionAsync(
                        token,
                        usuario.IdEmpleado);

                if (resultado.Exitoso)
                {
                    TempData["Mensaje"] =
                        "Se reenvió el enlace de activación al correo indicado.";
                }
                else
                {
                    Errores.Add(resultado.Mensaje);
                    CorreoDuplicado = correo;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al reenviar la activación por correo.");
                Errores.Add(
                    "No se pudo reenviar el enlace. Inténtalo nuevamente.");
            }

            return Page();
        }

        private async Task MarcarCorreoDuplicadoAsync(string? token)
        {
            if (!ErroresCampo.ContainsKey(
                    nameof(RegistroSolicitud.CorreoElectronico)))
            {
                return;
            }

            CorreoDuplicado = Solicitud.CorreoElectronico;

            try
            {
                RespuestaApi<List<UsuarioApi>> pendientes =
                    await _api.PendientesAsync(token);

                CorreoDuplicadoPendiente = pendientes.Datos?.Any(u =>
                    string.Equals(
                        u.CorreoElectronico,
                        CorreoDuplicado,
                        StringComparison.OrdinalIgnoreCase)) == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al comprobar si el correo duplicado está pendiente.");
            }
        }

        private void NormalizarDatos()
        {
            Solicitud.Nombre = NormalizarTexto(Solicitud.Nombre);
            Solicitud.Apellido = NormalizarTexto(Solicitud.Apellido);
            Solicitud.Telefono = NormalizarTexto(Solicitud.Telefono);
            Solicitud.CorreoElectronico =
                NormalizarTexto(Solicitud.CorreoElectronico);
            Solicitud.Rol = RolesSistema.Normalizar(Solicitud.Rol);
        }

        private static string NormalizarTexto(string? texto)
        {
            return Regex.Replace(texto?.Trim() ?? "", @"\s+", " ");
        }
    }
}
