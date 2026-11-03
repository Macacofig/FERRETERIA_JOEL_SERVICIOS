using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Helpers;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Pantalla pública de activación. El front descifra el token del enlace y
    /// delega la validación y el consumo en ServicioUsuarios; no guarda ni
    /// verifica contraseñas.
    /// </summary>
    [AllowAnonymous]
    public class ActivarCuentaModel : PageModel
    {
        private readonly UsuariosApiCliente _api;
        private readonly ILogger<ActivarCuentaModel> _logger;

        [BindProperty]
        public ActivacionSolicitud Solicitud { get; set; } = new();

        public string? Mensaje { get; set; }

        public string? NombreEmpleado { get; set; }

        public bool TokenValido { get; set; }

        public bool Activada { get; set; }

        public Dictionary<string, string> ErroresCampo { get; set; } = new();

        public ActivarCuentaModel(
            UsuariosApiCliente api,
            ILogger<ActivarCuentaModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string? token)
        {
            ViewData["ActiveMenu"] = "";

            string? tokenDescifrado = Descifrar(token);

            if (string.IsNullOrWhiteSpace(tokenDescifrado))
            {
                Mensaje = "El enlace de activación no es válido.";
                TokenValido = false;
                return Page();
            }

            try
            {
                RespuestaApi<ActivacionApiResultado> respuesta =
                    await _api.ValidarActivacionAsync(tokenDescifrado);

                ActivacionApiResultado? resultado = respuesta.Datos;

                if (resultado is null || !resultado.Exitoso)
                {
                    Mensaje = resultado?.Mensaje ?? respuesta.Mensaje;
                    TokenValido = false;
                    return Page();
                }

                TokenValido = true;
                NombreEmpleado = resultado.NombreEmpleado;
                Solicitud.Token = tokenDescifrado;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "No se pudo validar el enlace de activación.");
                Mensaje =
                    "No se pudo conectar con el servicio de usuarios. Inténtelo nuevamente.";
                TokenValido = false;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["ActiveMenu"] = "";

            string? tokenDescifrado = Descifrar(Solicitud.Token);

            if (string.IsNullOrWhiteSpace(tokenDescifrado))
            {
                Mensaje = "El enlace de activación no es válido.";
                TokenValido = false;
                return Page();
            }

            try
            {
                RespuestaApi<ActivacionApiResultado> respuesta =
                    await _api.ActivarAsync(
                        tokenDescifrado,
                        Solicitud.Contrasena,
                        Solicitud.ConfirmarContrasena);

                ActivacionApiResultado? resultado = respuesta.Datos;

                if (resultado is null || !resultado.Exitoso)
                {
                    Mensaje = resultado?.Mensaje ?? respuesta.Mensaje;
                    TokenValido = false;
                    return Page();
                }

                if (resultado.ErroresCampo.Any())
                {
                    TokenValido = true;
                    NombreEmpleado = resultado.NombreEmpleado;
                    ErroresCampo = resultado.ErroresCampo;
                    Solicitud.Token = tokenDescifrado;
                    Solicitud.Contrasena = string.Empty;
                    Solicitud.ConfirmarContrasena = string.Empty;
                    return Page();
                }

                TokenValido = false;
                Activada = true;
                NombreEmpleado = resultado.NombreEmpleado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo activar la cuenta.");
                Mensaje =
                    "No se pudo conectar con el servicio de usuarios. Inténtelo nuevamente.";
                TokenValido = false;
            }

            return Page();
        }

        private static string? Descifrar(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return UrlProtector.Descifrar(token) ?? token;
        }
    }
}
