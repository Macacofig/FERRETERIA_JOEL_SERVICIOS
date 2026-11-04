using System.Security.Claims;
using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.ServicioUsuarios.Contratos;
using FERRETERIA__Joel.Aplicacion.Servicios;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FERRETERIA__Joel.ServicioUsuarios.Controllers
{
    /// <summary>
    /// CRUD de usuarios, activación por correo, cambio de contraseña y
    /// bitácora. Todo el acceso a datos pasa por los servicios de aplicación
    /// existentes; el controlador solo traduce HTTP ↔ casos de uso y audita.
    /// </summary>
    [ApiController]
    [Route("api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly ServicioRegistroEmpleado _registro;
        private readonly ServicioEdicionEmpleado _edicion;
        private readonly ServicioPersonal _personal;
        private readonly ServicioActivacionCuenta _activacion;
        private readonly ServicioCambioContrasena _cambioContrasena;
        private readonly ServicioBitacora _bitacora;
        private readonly IRevocacionTokens _revocacion;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(
            ServicioRegistroEmpleado registro,
            ServicioEdicionEmpleado edicion,
            ServicioPersonal personal,
            ServicioActivacionCuenta activacion,
            ServicioCambioContrasena cambioContrasena,
            ServicioBitacora bitacora,
            IRevocacionTokens revocacion,
            ILogger<UsuariosController> logger)
        {
            _registro = registro;
            _edicion = edicion;
            _personal = personal;
            _activacion = activacion;
            _cambioContrasena = cambioContrasena;
            _bitacora = bitacora;
            _revocacion = revocacion;
            _logger = logger;
        }

        // ===================== Consultas =====================

        /// <summary>Listado de personal (proyección sin credenciales).</summary>
        [HttpGet]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Listar()
        {
            List<Empleado> empleados =
                await _personal.ListarAsync();

            return Ok(RespuestaApi<List<UsuarioDto>>.Ok(
                empleados.Select(MapeosDto.AUsuario).ToList()));
        }

        /// <summary>Cuentas pendientes de activación.</summary>
        [HttpGet("pendientes")]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Pendientes()
        {
            List<Empleado> empleados =
                await _personal.ListarPendientesAsync();

            return Ok(RespuestaApi<List<UsuarioDto>>.Ok(
                empleados.Select(MapeosDto.AUsuario).ToList()));
        }

        /// <summary>Consulta una cuenta por identificador.</summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Obtener(int id)
        {
            Empleado? empleado =
                await _personal.ObtenerPorIdAsync(id);

            if (empleado is null)
            {
                return NotFound(RespuestaApi<UsuarioDto>.Error(
                    "El empleado no existe."));
            }

            return Ok(RespuestaApi<UsuarioDto>.Ok(
                MapeosDto.AUsuario(empleado)));
        }

        // ===================== CRUD =====================

        /// <summary>
        /// Alta de empleado. Genera el nombre de usuario único, deja la cuenta
        /// pendiente de activación y envía el enlace de un solo uso por correo.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Crear([FromBody] CrearUsuarioRequest solicitud)
        {
            if (solicitud is null)
            {
                return BadRequest(RespuestaApi<UsuarioDto>.Error(
                    "Los datos del empleado son obligatorios."));
            }

            ResultadoRegistro resultado = await _registro.RegistrarAsync(
                new RegistroSolicitud
                {
                    Nombre = solicitud.Nombre,
                    Apellido = solicitud.Apellido,
                    Telefono = solicitud.Telefono,
                    CorreoElectronico = solicitud.CorreoElectronico,
                    Rol = solicitud.Rol
                });

            await AuditarAsync(
                ServicioBitacora.OperacionInsert,
                "empleado",
                resultado.Exitoso ? resultado.Id : null,
                resultado.Exitoso);

            if (!resultado.Exitoso)
            {
                return BadRequest(RespuestaApi<UsuarioDto>
                    .ConErrores(resultado.ErroresCampo, "No se pudo crear el empleado."));
            }

            Empleado? empleado =
                await _personal.ObtenerPorIdAsync(resultado.Id);

            return CreatedAtAction(
                nameof(Obtener),
                new { id = resultado.Id },
                RespuestaApi<object>.Ok(new
                {
                    resultado.Id,
                    resultado.NombreUsuario,
                    resultado.EnlaceActivacion,
                    Usuario = empleado is null ? null : MapeosDto.AUsuario(empleado)
                }, "Empleado registrado. Se envió el enlace de activación a su correo."));
        }

        /// <summary>
        /// Edición de datos personales. No toca contraseña, estado ni token de
        /// activación; conserva el comportamiento de autenticación.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Actualizar(
            int id,
            [FromBody] ActualizarUsuarioRequest solicitud)
        {
            if (solicitud is null)
            {
                return BadRequest(RespuestaApi<UsuarioDto>.Error(
                    "Los datos del empleado son obligatorios."));
            }

            ResultadoRegistro resultado = await _edicion.ActualizarAsync(
                id,
                new EdicionEmpleadoSolicitud
                {
                    Nombre = solicitud.Nombre,
                    Apellido = solicitud.Apellido,
                    Telefono = solicitud.Telefono,
                    CorreoElectronico = solicitud.CorreoElectronico,
                    Rol = solicitud.Rol
                },
                ObtenerIdEmpleado());

            await AuditarAsync(
                ServicioBitacora.OperacionUpdate,
                "empleado",
                id,
                resultado.Exitoso);

            if (!resultado.Exitoso)
            {
                return BadRequest(RespuestaApi<UsuarioDto>
                    .ConErrores(resultado.ErroresCampo, "No se pudo actualizar el empleado."));
            }

            Empleado? empleado =
                await _personal.ObtenerPorIdAsync(id);

            return Ok(RespuestaApi<UsuarioDto>.Ok(
                MapeosDto.AUsuario(empleado!),
                "Empleado actualizado."));
        }

        /// <summary>
        /// Baja lógica: la cuenta pasa a inactiva, se registra el responsable y
        /// se invalidan los tokens emitidos. No se borra la fila.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> DarDeBaja(int id)
        {
            ResultadoPersonal resultado = await _personal.DarDeBajaAsync(id);

            await AuditarAsync(
                ServicioBitacora.OperacionDelete,
                "empleado",
                id,
                resultado.Exitoso);

            if (!resultado.Exitoso)
            {
                return BadRequest(RespuestaApi<object>.Error(resultado.Mensaje));
            }

            _revocacion.RevocarUsuario(id);

            return Ok(RespuestaApi<object>.Ok(
                new { resultado.IdEmpleado, resultado.Estado },
                resultado.Mensaje));
        }

        /// <summary>
        /// Reactivación: la cuenta vuelve a pendiente de activación y se le
        /// envía un enlace nuevo.
        /// </summary>
        [HttpPost("{id:int}/reactivar")]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Reactivar(int id)
        {
            ResultadoPersonal resultado = await _personal.ReactivarAsync(id);

            await AuditarAsync(
                ServicioBitacora.OperacionUpdate,
                "empleado",
                id,
                resultado.Exitoso);

            if (!resultado.Exitoso)
            {
                return BadRequest(RespuestaApi<object>.Error(resultado.Mensaje));
            }

            _revocacion.RevocarUsuario(id);

            return Ok(RespuestaApi<object>.Ok(
                new { resultado.IdEmpleado, resultado.Estado },
                resultado.Mensaje));
        }

        /// <summary>Reenvía el enlace de activación de una cuenta pendiente.</summary>
        [HttpPost("{id:int}/reenviar-activacion")]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> ReenviarActivacion(int id)
        {
            ResultadoPersonal resultado =
                await _personal.ReenviarActivacionAsync(id);

            await AuditarAsync(
                ServicioBitacora.OperacionUpdate,
                "empleado",
                id,
                resultado.Exitoso);

            if (!resultado.Exitoso)
            {
                return BadRequest(RespuestaApi<object>.Error(resultado.Mensaje));
            }

            return Ok(RespuestaApi<object>.Ok(
                new { resultado.IdEmpleado, resultado.Estado },
                resultado.Mensaje));
        }

        // ===================== Activación =====================

        /// <summary>Valida un token de activación sin consumirlo.</summary>
        [HttpPost("activar/validar")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarActivacion(
            [FromBody] ValidarActivacionRequest solicitud)
        {
            ResultadoActivacion resultado =
                await _activacion.ValidarTokenAsync(solicitud?.Token);

            return Ok(RespuestaApi<ResultadoActivacion>.Ok(
                resultado,
                resultado.Mensaje));
        }

        /// <summary>
        /// Consume el token, define la contraseña con hash Argon2id y activa la
        /// cuenta. El token es de un solo uso y vence a los 10 minutos.
        /// </summary>
        [HttpPost("activar")]
        [AllowAnonymous]
        public async Task<IActionResult> Activar([FromBody] ActivacionRequest solicitud)
        {
            if (solicitud is null)
            {
                return BadRequest(RespuestaApi<ResultadoActivacion>.Error(
                    "Los datos de activación son obligatorios."));
            }

            ResultadoActivacion resultado = await _activacion.ActivarAsync(
                new ActivacionSolicitud
                {
                    Token = solicitud.Token,
                    Contrasena = solicitud.Contrasena,
                    ConfirmarContrasena = solicitud.ConfirmarContrasena
                });

            return Ok(RespuestaApi<ResultadoActivacion>.Ok(
                resultado,
                resultado.Mensaje));
        }

        // ===================== Contraseña =====================

        /// <summary>
        /// Cambio de contraseña del usuario autenticado. Verifica la actual,
        /// aplica la política y revoca los tokens previos.
        /// </summary>
        [HttpPost("cambiar-contrasena")]
        [Authorize]
        public async Task<IActionResult> CambiarContrasena(
            [FromBody] CambioContrasenaRequest solicitud)
        {
            int id = ObtenerIdEmpleado();

            if (id <= 0)
            {
                return Unauthorized(RespuestaApi<object>.Error(
                    "La sesión no es válida."));
            }

            ResultadoCambioContrasena resultado =
                await _cambioContrasena.CambiarAsync(
                    id,
                    new CambioContrasenaSolicitud
                    {
                        ContrasenaActual = solicitud?.ContrasenaActual ?? string.Empty,
                        ContrasenaNueva = solicitud?.ContrasenaNueva ?? string.Empty,
                        ConfirmarContrasena = solicitud?.ConfirmarContrasena ?? string.Empty
                    });

            await AuditarAsync(
                ServicioBitacora.OperacionUpdate,
                "empleado",
                id,
                resultado.Exitoso);

            if (!resultado.Exitoso)
            {
                return BadRequest(new RespuestaApi<object>
                {
                    Exitoso = false,
                    Mensaje = resultado.Mensaje,
                    Errores = new List<string> { resultado.Mensaje },
                    ErroresCampo = resultado.ErroresCampo
                });
            }

            // La contraseña anterior deja de ser válida y las sesiones abiertas
            // con ella se invalidan.
            _revocacion.RevocarUsuario(id);

            return Ok(RespuestaApi<object>.Ok(new { }, resultado.Mensaje));
        }

        // ===================== Auxiliares =====================

        private int ObtenerIdEmpleado()
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int id) ? id : 0;
        }

        private async Task AuditarAsync(
            string operacion,
            string entidad,
            int? idRegistro,
            bool exitoso)
        {
            try
            {
                int id = ObtenerIdEmpleado();

                await _bitacora.RegistrarOperacionAsync(
                    operacion,
                    entidad,
                    idRegistro,
                    exitoso,
                    id > 0 ? id : null,
                    User.FindFirstValue(ClaimTypes.Name),
                    User.FindFirstValue(ClaimTypes.Role) ?? RolesSistema.EmpleadoVenta);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "No se pudo registrar {Operacion} sobre {Entidad} en la bitácora.",
                    operacion,
                    entidad);
            }
        }

        private static bool OperacionValida(string? operacion)
        {
            return string.Equals(operacion, ServicioBitacora.OperacionInsert, StringComparison.OrdinalIgnoreCase)
                || string.Equals(operacion, ServicioBitacora.OperacionUpdate, StringComparison.OrdinalIgnoreCase)
                || string.Equals(operacion, ServicioBitacora.OperacionDelete, StringComparison.OrdinalIgnoreCase);
        }
    }
}

