using FERRETERIA__Joel.Dominio.Entidades;

namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Traduce entidades del dominio a DTOs pÃºblicos. Es el Ãºnico punto que
    /// construye la proyecciÃ³n que sale por HTTP: no copia contraseÃ±a, hash ni
    /// token de activaciÃ³n.
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


