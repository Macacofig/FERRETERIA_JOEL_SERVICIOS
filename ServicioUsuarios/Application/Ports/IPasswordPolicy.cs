namespace ServicioUsuarios.Application.Ports;

public interface IPasswordPolicy
{
    bool EsValida(string password);
    string ObtenerMensaje();
}
