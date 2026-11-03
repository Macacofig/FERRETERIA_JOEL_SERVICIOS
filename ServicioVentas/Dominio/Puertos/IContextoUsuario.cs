namespace ServicioVentas.Dominio.Puertos
{
    /// <summary>
    /// Punto de integración con la autenticación del equipo. El módulo de
    /// Ventas NO implementa login, roles ni sesiones: solo consume la
    /// identidad ya resuelta a través de este puerto.
    ///
    /// La UI envía un JWT token en cada request. La capa Web resuelve la
    /// identidad desde los claims del token y la provee a través de este
    /// puerto. El dominio depende de la abstracción (no de ASP.NET).
    ///
    /// Contrato esperado:
    /// <list type="bullet">
    /// <item><c>IdEmpleado</c>: identificador del empleado autenticado; devuelve 0 cuando no hay autenticación.</item>
    /// <item><c>NombreCompleto</c>: nombre para mostrar.</item>
    /// <item><c>Rol</c>: rol del usuario (Administrador o EmpleadoVenta).</item>
    /// </list>
    /// </summary>
    public interface IContextoUsuario
    {
        int IdEmpleado { get; }

        string NombreCompleto { get; }

        string Rol { get; }
    }
}
