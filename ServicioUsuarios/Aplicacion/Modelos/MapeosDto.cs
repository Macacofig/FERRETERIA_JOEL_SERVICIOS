using FERRETERIA__Joel.Dominio.Entidades;

namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Traduce entidades del dominio a DTOs públicos. Es el único punto que
    /// construye la proyección que sale por HTTP: no copia contraseña, hash ni
    /// token de activación.
    /// </summary>
    public static class MapeosDto
    {
        public static UsuarioDto AUsuario(Empleado empleado)
        {
            return new UsuarioDto
            {
                IdEmpleado = empleado.IdEmpleado,
                Nombre = empleado.Nombre,
                Apellido = empleado.Apellido,
                NombreUsuario = empleado.NombreUsuario,
                Rol = empleado.Rol,
                Telefono = empleado.Telefono,
                CorreoElectronico = empleado.CorreoElectronico,
                Estado = empleado.Estado,
                FechaRegistro = empleado.FechaRegistro,
                FechaActualizacion = empleado.FechaActualizacion,
                IdEmpleadoResponsable = empleado.IdEmpleadoResponsable
            };
        }
    }
}


