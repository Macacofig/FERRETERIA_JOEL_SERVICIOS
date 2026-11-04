using System.Security.Claims;
using FERRETERIA__Joel.Aplicacion.Servicios;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.ServicioUsuarios.Contratos;
using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Puertos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FERRETERIA__Joel.ServicioUsuarios.Controllers
{
    [ApiController]
    [Route("api/usuarios/bitacora")]
    public class BitacoraController : ControllerBase
    {
        private readonly ServicioBitacora _bitacora;
        private readonly IContextoUsuario _contextoUsuario;

        public BitacoraController(ServicioBitacora bitacora, IContextoUsuario contextoUsuario)
        {
            _bitacora = bitacora;
            _contextoUsuario = contextoUsuario;
        }

        [HttpPost]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Registrar(
            [FromBody] BitacoraRequest solicitud)
        {
            if (solicitud is null
                || !OperacionValida(solicitud.Operacion)
                || string.IsNullOrWhiteSpace(solicitud.Entidad))
            {
                return BadRequest(RespuestaApi<object>.Error(
                    "La operación debe ser INSERT, UPDATE o DELETE e indicar la entidad."));
            }

            int id = _contextoUsuario.IdEmpleado;
            await _bitacora.RegistrarOperacionAsync(
                solicitud.Operacion.Trim().ToUpperInvariant(),
                solicitud.Entidad.Trim(),
                solicitud.IdRegistroAfectado,
                solicitud.Exitoso,
                id > 0 ? id : null,
                User.FindFirstValue(ClaimTypes.Name),
                User.FindFirstValue(ClaimTypes.Role)
                    ?? RolesSistema.EmpleadoVenta);

            return Ok(RespuestaApi<object>.Ok(
                new { },
                "Operación registrada en la bitácora."));
        }

        [HttpGet]
        [Authorize(Roles = RolesSistema.Administrador)]
        public async Task<IActionResult> Listar([FromQuery] int limite = 100)
        {
            List<RegistroBitacora> registros =
                await _bitacora.ListarAsync(limite);

            return Ok(RespuestaApi<List<RegistroBitacora>>.Ok(registros));
        }

        private static bool OperacionValida(string? operacion)
        {
            return string.Equals(operacion, ServicioBitacora.OperacionInsert,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(operacion, ServicioBitacora.OperacionUpdate,
                       StringComparison.OrdinalIgnoreCase)
                || string.Equals(operacion, ServicioBitacora.OperacionDelete,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
