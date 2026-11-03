namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Punto de integraciÃ³n con la autenticaciÃ³n del equipo. El mÃ³dulo de
    /// Ventas NO implementa login, roles ni sesiones: solo consume la
    /// identidad ya resuelta a travÃ©s de este puerto. La aplicaciÃ³n y el
    /// dominio dependen de la abstracciÃ³n (no de ASP.NET); el adaptador
    /// Web la resuelve desde las claims del usuario autenticado.
    ///
    /// Contrato esperado para el mÃ³dulo de seguridad:
    /// <list type="bullet">
    /// <item><c>IdEmpleado</c>: identificador del empleado autenticado;
    /// devuelve 0 cuando no hay autenticaciÃ³n (Ventas rechaza registrar o
    /// anular venta si es menor o igual a 0).</item>
    /// <item><c>NombreCompleto</c>: nombre para mostrar.</item>
        /// <item><c>Rol</c>: rol del usuario; Ventas permite la anulaciÃ³n al
        /// rol Empleado, pero la autorizaciÃ³n definitiva corresponde al mÃ³dulo
        /// de seguridad.</item>
    /// </list>
    /// Mientras ese mÃ³dulo no exista, la capa Web aporta el adaptador
    /// temporal <c>ContextoUsuarioHttp</c>, reemplazable sin tocar Ventas.
    /// </summary>
    public interface IContextoUsuario
    {
        int IdEmpleado { get; }

        string NombreCompleto { get; }

        string Rol { get; }
    }
}

