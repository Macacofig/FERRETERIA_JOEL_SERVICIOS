namespace FERRETERIA__Joel.Dominio.Constantes
{
    /// <summary>
    /// Roles del sistema usados en la autenticaciÃ³n y en las polÃ­ticas de
    /// autorizaciÃ³n. Solo el personal interno accede al sistema; los clientes
    /// se registran dentro del flujo de venta y no inician sesiÃ³n.
    /// </summary>
    public static class RolesSistema
    {
        // Gestiona el catÃ¡logo y el inventario. En ventas solo consulta el
        // historial y el detalle: no registra ni anula.
        public const string Administrador = "Administrador";

        // Registra ventas, consulta el historial y puede anular ventas.
        public const string EmpleadoVenta = "EmpleadoVenta";

        // Lista de roles de personal admitidos por la autorizaciÃ³n por rol.
        public const string Personal = Administrador + "," + EmpleadoVenta;

        /// <summary>
        /// Normaliza el rol almacenado en la tabla <c>empleado</c> a uno de
        /// los dos roles vÃ¡lidos. Cualquier valor distinto de "Administrador"
        /// se trata como <see cref="EmpleadoVenta"/>.
        /// </summary>
        public static string Normalizar(string rol)
        {
            return string.Equals(
                rol?.Trim(),
                Administrador,
                StringComparison.OrdinalIgnoreCase)
                ? Administrador
                : EmpleadoVenta;
        }
    }
}

