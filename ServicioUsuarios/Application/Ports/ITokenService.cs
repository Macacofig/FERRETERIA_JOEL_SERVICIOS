using ServicioUsuarios.Domain.Entities;

namespace ServicioUsuarios.Application.Ports;

public interface ITokenService
{
    string CrearToken(Usuario usuario);
}
