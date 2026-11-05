namespace FERRETERIA__Joel.Dominio.Utilidades;

public interface ICredencialesValidaciones
{
    bool EsNombreValido(string? nombre);
    bool EsApellidoValido(string? apellido);
    bool EsTelefonoValido(string? telefono);
    bool EsCorreoValido(string? correo);
    bool EsContrasenaValida(string? contrasena);
    bool EsConfirmacionValida(string contrasena, string confirmacion);
}
