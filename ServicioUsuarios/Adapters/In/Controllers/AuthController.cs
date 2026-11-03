using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioUsuarios.Application.UseCases;

namespace ServicioUsuarios.Adapters.In.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(LoginUseCase loginUseCase) : ControllerBase
{
    public sealed record LoginRequest(string NombreUsuario, string Password);

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        string? token = await loginUseCase.EjecutarAsync(request.NombreUsuario, request.Password, cancellationToken);
        return token is null
            ? Unauthorized(new { mensaje = "Credenciales inválidas." })
            : Ok(new { token });
    }

    public sealed record ActivarRequest(string Password, string PasswordConfirmacion);

    [AllowAnonymous]
    [HttpPost("activate")]
    public async Task<IActionResult> Activar([FromQuery] string token, [FromBody] ActivarRequest request, [FromServices] UsuariosUseCase usuariosUseCase, CancellationToken cancellationToken)
    {
        var resultado = await usuariosUseCase.ActivarAsync(token, request.Password, request.PasswordConfirmacion, cancellationToken);
        return resultado.Exito ? Ok(new { mensaje = resultado.Mensaje }) : BadRequest(new { mensaje = resultado.Mensaje });
    }
}
