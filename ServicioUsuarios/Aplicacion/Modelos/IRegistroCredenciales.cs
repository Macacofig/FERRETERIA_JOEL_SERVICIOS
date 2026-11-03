namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Campos comunes al alta de personal autorizada por un administrador.
    /// La contraseña ya no forma parte del alta: el empleado la define al
    /// abrir el enlace de activación que recibe por correo.
    /// </summary>
    public interface IRegistroCredenciales
    {
        string Nombre { get; }

        string Apellido { get; }

        string Telefono { get; }

        string CorreoElectronico { get; }

        /// <summary>Rol a asignar: Administrador o EmpleadoVenta.</summary>
        string Rol { get; }
    }
}
