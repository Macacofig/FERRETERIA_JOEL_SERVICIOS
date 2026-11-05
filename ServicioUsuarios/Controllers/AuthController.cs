using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Aplicacion.Servicios;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FERRETERIA__Joel.ServicioUsuarios.Controllers
{
    /// <summary>
    /// Autenticación y sesión. Emite JWT Bearer y expone la identidad actual.
    /// El front Razor es el único cliente: guarda el token en su sesión de
    /// servidor y nunca lo expone a JavaScript ni a localStorage.
    /// </summary>
    [ApiController]
    [Route("api/usuarios")]
    public class AuthController : ControllerBase
    {
        private readonly ServicioInicioSesion _inicioSesion;
        private readonly ServicioBitacora _bitacora;
        private readonly ServicioPersonal _personal;
        private readonly IEmisorToken _jwt;
        private readonly IRevocacionTokens _revocacion;
        private readonly ILogger<AuthController> _logger;
        private readonly IContextoUsuario _contextoUsuario;

        public AuthController(
            ServicioInicioSesion inicioSesion,
            ServicioBitacora bitacora,
            ServicioPersonal personal,
            IEmisorToken jwt,
            IRevocacionTokens revocacion,
            ILogger<AuthController> logger,
            IContextoUsuario contextoUsuario)
        {
            _inicioSesion = inicioSesion;
            _bitacora = bitacora;
            _personal = personal;
            _jwt = jwt;
            _revocacion = revocacion;
            _logger = logger;
            _contextoUsuario = contextoUsuario;
        }

        /// <summary>
        /// Inicia sesión con correo o nombre de usuario y contraseña.
        /// Devuelve el JWT y la proyección pública del usuario autenticado.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest solicitud)
        {
            if (solicitud is null
                || string.IsNullOrWhiteSpace(solicitud.CorreoElectronico)
                || string.IsNullOrWhiteSpace(solicitud.Contrasena))
            {
                return BadRequest(RespuestaApi<LoginResponse>.Error(
                    "Ingrese su correo o usuario y su contraseña."));
            }

            var resultado = await _inicioSesion.IniciarAsync(
                new InicioSesionSolicitud
                {
                    CorreoElectronico = solicitud.CorreoElectronico,
                    Contrasena = solicitud.Contrasena
                });

            if (!resultado.Exitoso || resultado.Usuario is null)
            {
                try
                {
                    await _bitacora.RegistrarLoginFallidoAsync(
                        solicitud.CorreoElectronico);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo registrar el intento fallido de inicio de sesión.");
                }
                return Unauthorized(RespuestaApi<LoginResponse>.Error(
                    resultado.Mensaje));
            }

            var usuario = resultado.Usuario;

            (string token, DateTime expira) = _jwt.Emitir(
                usuario.Id,
                usuario.NombreCompleto,
                usuario.CorreoElectronico,
                usuario.Rol,
                usuario.NombreUsuario);

            try
            {
                await _bitacora.RegistrarLoginAsync(
                    usuario.Id,
                    usuario.NombreCompleto,
                    usuario.Rol);
            }
            catch (Exception ex)
            {
                // La bitácora no debe impedir el inicio de sesión, pero sí
                // quedar registrada en el log de la aplicación.
                _logger.LogError(ex, "No se pudo registrar el login en la bitácora.");
            }

            Empleado? empleado = await _personal
                .ObtenerPorIdAsync(usuario.Id);

            LoginResponse respuesta = new()
            {
                Token = token,
                Expira = expira,
                Usuario = empleado is not null
                    ? MapeosDto.AUsuario(empleado)
                    : new UsuarioDto
                    {
                        IdEmpleado = usuario.Id,
                        Nombre = usuario.NombreCompleto,
                        NombreUsuario = usuario.NombreUsuario,
                        Rol = usuario.Rol,
                        CorreoElectronico = usuario.CorreoElectronico
                    }
            };

            return Ok(RespuestaApi<LoginResponse>.Ok(respuesta));
        }

        /// <summary>
        /// Cierra la sesión: revoca el token actual y registra el evento.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            string? jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            DateTime expira = DateTime.UtcNow.AddMinutes(_jwt.DuracionMinutos);

            if (Request.Headers.TryGetValue("Authorization", out var encabezado))
            {
                string valor = encabezado.ToString();

                if (valor.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        JwtSecurityToken token = new JwtSecurityTokenHandler()
                            .ReadJwtToken(valor["Bearer ".Length..].Trim());

                        expira = token.ValidTo == DateTime.MinValue
                            ? expira
                            : token.ValidTo;
                    }
                    catch
                    {
                        // Si el token no se puede leer, se revoca hasta la
                        // expiración máxima configurada.
                    }
                }
            }

            _revocacion.Revocar(jti, expira);

            try
            {
                await _bitacora.RegistrarLogoutAsync(
                    _contextoUsuario.IdEmpleado,
                    User.FindFirstValue(ClaimTypes.Name),
                    User.FindFirstValue(ClaimTypes.Role) ?? RolesSistema.EmpleadoVenta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo registrar el logout en la bitácora.");
            }

            return Ok(RespuestaApi<object>.Ok(new { }, "Sesión cerrada."));
        }

        /// <summary>
        /// Devuelve la identidad actual releyendo el estado de la cuenta. Si la
        /// cuenta fue dada de baja, la respuesta es 401 y el front invalida su
        /// cookie: el bloqueo es inmediato aunque el JWT siga vigente.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            int id = _contextoUsuario.IdEmpleado;

            if (id <= 0)
            {
                return Unauthorized(RespuestaApi<UsuarioDto>.Error(
                    "La sesión no es válida."));
            }

            Empleado? empleado =
                await _personal.ObtenerPorIdAsync(id);

            if (empleado is null || empleado.Estado != EstadoEmpleado.Activo)
            {
                return Unauthorized(RespuestaApi<UsuarioDto>.Error(
                    "La cuenta no está activa."));
            }

            return Ok(RespuestaApi<UsuarioDto>.Ok(
                MapeosDto.AUsuario(empleado)));
        }

    }
}
