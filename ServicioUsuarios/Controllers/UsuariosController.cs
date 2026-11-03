using System.Security.Claims;
using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Aplicacion.Servicios;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.ServicioUsuarios.Contratos;
using FERRETERIA__Joel.ServicioUsuarios.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FERRETERIA__Joel.ServicioUsuarios.Controllers
{
    /// <summary>
    /// CRUD de usuarios, activaciÃ³n por correo, cambio de contraseÃ±a y
    /// bitÃ¡cora. Todo el acceso a datos pasa por los servicios de aplicaciÃ³n
    /// existentes; el controlador solo traduce HTTP â†” casos de uso y audita.
    /// </summary>
    [ApiController]
    [Route("api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly IRepositoryEmpleado _repositorio;
        private readonly ServicioRegistroEmpleado _registro;
        private readonly ServicioEdicionEmpleado _edicion;
        private readonly ServicioPersonal _personal;
        private readonly ServicioActivacionCuenta _activacion;
        private readonly ServicioCambioContrasena _cambioContrasena;
        private readonly ServicioBitacora _bitacora;
        private readonly RevocacionTokens _revocacion;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(
            IRepositoryEmpleado repositorio,
            ServicioRegistroEmpleado registro,
            ServicioEdicionEmpleado edicion,
            ServicioPersonal personal,
            ServicioActivacionCuenta activacion,
            ServicioCambioContrasena cambioContrasena,
            ServicioBitacora bitacora,
            RevocacionTokens revocacion,
            ILogger<UsuariosController> logger)
        {
            _repositorio = repositorio;
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

        /// <summary>Listado de personal (proyecciÃ³n sin credenciales).</summary>
        [HttpGet]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Listar()
        {
            List<Empleado> empleados =
                await _repositorio.ObtenerTodosSinCredencialesAsync();

            return Ok(RespuestaApi<List<UsuarioDto>>.Ok(
                empleados.Select(MapeosDto.AUsuario).ToList()));
        }

        /// <summary>Cuentas pendientes de activaciÃ³n.</summary>
        [HttpGet("pendientes")]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Pendientes()
        {
            List<Empleado> empleados =
                await _repositorio.ObtenerPendientesActivacionAsync();

            return Ok(RespuestaApi<List<UsuarioDto>>.Ok(
                empleados.Select(MapeosDto.AUsuario).ToList()));
        }

        /// <summary>Consulta una cuenta por identificador.</summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Obtener(int id)
        {
            Empleado? empleado =
                await _repositorio.ObtenerPorIdSinCredencialesAsync(id);

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
        /// Alta de empleado. Genera el nombre de usuario Ãºnico, deja la cuenta
        /// pendiente de activaciÃ³n y envÃ­a el enlace de un solo uso por correo.
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
                await _repositorio.ObtenerPorIdSinCredencialesAsync(resultado.Id);

            return CreatedAtAction(
                nameof(Obtener),
                new { id = resultado.Id },
                RespuestaApi<object>.Ok(new
                {
                    resultado.Id,
                    resultado.NombreUsuario,
                    resultado.EnlaceActivacion,
                    Usuario = empleado is null ? null : MapeosDto.AUsuario(empleado)
                }, "Empleado registrado. Se enviÃ³ el enlace de activaciÃ³n a su correo."));
        }

        /// <summary>
        /// EdiciÃ³n de datos personales. No toca contraseÃ±a, estado ni token de
        /// activaciÃ³n; conserva el comportamiento de autenticaciÃ³n.
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
                await _repositorio.ObtenerPorIdSinCredencialesAsync(id);

            return Ok(RespuestaApi<UsuarioDto>.Ok(
                MapeosDto.AUsuario(empleado!),
                "Empleado actualizado."));
        }

        /// <summary>
        /// Baja lÃ³gica: la cuenta pasa a inactiva, se registra el responsable y
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
        /// ReactivaciÃ³n: la cuenta vuelve a pendiente de activaciÃ³n y se le
        /// envÃ­a un enlace nuevo.
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

        /// <summary>ReenvÃ­a el enlace de activaciÃ³n de una cuenta pendiente.</summary>
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

        // ===================== ActivaciÃ³n =====================

        /// <summary>Valida un token de activaciÃ³n sin consumirlo.</summary>
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
        /// Consume el token, define la contraseÃ±a con hash Argon2id y activa la
        /// cuenta. El token es de un solo uso y vence a los 10 minutos.
        /// </summary>
        [HttpPost("activar")]
        [AllowAnonymous]
        public async Task<IActionResult> Activar([FromBody] ActivacionRequest solicitud)
        {
            if (solicitud is null)
            {
                return BadRequest(RespuestaApi<ResultadoActivacion>.Error(
                    "Los datos de activaciÃ³n son obligatorios."));
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

        // ===================== ContraseÃ±a =====================

        /// <summary>
        /// Cambio de contraseÃ±a del usuario autenticado. Verifica la actual,
        /// aplica la polÃ­tica y revoca los tokens previos.
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
                    "La sesiÃ³n no es vÃ¡lida."));
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

            // La contraseÃ±a anterior deja de ser vÃ¡lida y las sesiones abiertas
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
                    "No se pudo registrar {Operacion} sobre {Entidad} en la bitÃ¡cora.",
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


