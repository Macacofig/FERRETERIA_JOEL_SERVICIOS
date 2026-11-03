namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Punto de integración con la autenticación del equipo. El módulo de
    /// Ventas NO implementa login, roles ni sesiones: solo consume la
    /// identidad ya resuelta a través de este puerto. La aplicación y el
    /// dominio dependen de la abstracción (no de ASP.NET); el adaptador
    /// Web la resuelve desde las claims del usuario autenticado.
    ///
    /// Contrato esperado para el módulo de seguridad:
    /// <list type="bullet">
    /// <item><c>IdEmpleado</c>: identificador del empleado autenticado;
    /// devuelve 0 cuando no hay autenticación (Ventas rechaza registrar o
    /// anular venta si es menor o igual a 0).</item>
    /// <item><c>NombreCompleto</c>: nombre para mostrar.</item>
        /// <item><c>Rol</c>: rol del usuario; Ventas permite la anulación al
        /// rol Empleado, pero la autorización definitiva corresponde al módulo
        /// de seguridad.</item>
    /// </list>
    /// Mientras ese módulo no exista, la capa Web aporta el adaptador
    /// temporal <c>ContextoUsuarioHttp</c>, reemplazable sin tocar Ventas.
    /// </summary>
    public interface IContextoUsuario
    {
        int IdEmpleado { get; }

        string NombreCompleto { get; }

        string Rol { get; }
    }
}
