using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioUsuarios.Application.UseCases;
using ServicioUsuarios.Domain.Entities;

namespace ServicioUsuarios.Adapters.In.Controllers;

[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosController(UsuariosUseCase usuariosUseCase) : ControllerBase
{
    public sealed record UsuarioResponse(long Id, string NombreCompleto, string NombreUsuario, string Correo, string Rol, bool Activo, bool DebeCambiarPassword);
    public sealed record CrearUsuarioRequest(string NombreCompleto, string Correo, string Rol);
    public sealed record EditarUsuarioRequest(string NombreCompleto, string Correo, string Rol);
    public sealed record CambiarPasswordRequest(string PasswordActual, string PasswordNueva, string PasswordConfirmacion);

    private static UsuarioResponse Map(Usuario usuario) => new(usuario.Id, usuario.NombreCompleto, usuario.NombreUsuario, usuario.Correo, usuario.Rol, usuario.Activo, usuario.DebeCambiarPassword);

    private string Actor
    {
        get
        {
            string? nombre = User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("unique_name")
                ?? User.FindFirstValue(JwtRegisteredClaimNames.UniqueName);
            if (!string.IsNullOrWhiteSpace(nombre))
                return nombre;

            string? identificador = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(identificador) ? string.Empty : $"usuario:{identificador}";
        }
    }
    private long? ActorId
    {
        get
        {
            string? value = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(value, out long id) ? id : null;
        }
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Listar(CancellationToken cancellationToken)
    {
        var usuarios = await usuariosUseCase.ListarAsync(cancellationToken);
        return Ok(usuarios.Select(Map));
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<UsuarioResponse>> Obtener(long id, CancellationToken cancellationToken)
    {
        Usuario? usuario = await usuariosUseCase.ObtenerAsync(id, cancellationToken);
        return usuario is null ? NotFound() : Ok(Map(usuario));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioRequest request, CancellationToken cancellationToken)
    {
        var resultado = await usuariosUseCase.CrearAsync(request.NombreCompleto, request.Correo, request.Rol, Actor, cancellationToken);
        if (!resultado.Exito)
            return BadRequest(new { mensaje = resultado.Mensaje });
        return StatusCode(StatusCodes.Status201Created, Map(resultado.Usuario!));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Editar(long id, [FromBody] EditarUsuarioRequest request, CancellationToken cancellationToken)
    {
        bool editado = await usuariosUseCase.EditarAsync(id, request.NombreCompleto, request.Correo, request.Rol, Actor, cancellationToken);
        return editado ? NoContent() : NotFound();
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Eliminar(long id, CancellationToken cancellationToken)
    {
        bool eliminado = await usuariosUseCase.EliminarAsync(id, Actor, cancellationToken);
        return eliminado ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request, CancellationToken cancellationToken)
    {
        if (ActorId is null)
            return Unauthorized(new { mensaje = "No fue posible identificar al usuario autenticado." });

        var resultado = await usuariosUseCase.CambiarPasswordAsync(
            ActorId.Value,
            request.PasswordActual,
            request.PasswordNueva,
            request.PasswordConfirmacion,
            Actor,
            cancellationToken);

        return resultado.Exito
            ? Ok(new { mensaje = resultado.Mensaje })
            : BadRequest(new { mensaje = resultado.Mensaje });
    }
}
