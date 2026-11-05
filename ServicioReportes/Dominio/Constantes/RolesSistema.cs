namespace ServicioReportes.Dominio.Constantes
{
    /// <summary>
    /// Roles del sistema. Los reportes están disponibles para todo el personal
    /// interno (Administrador y EmpleadoVenta); la UI decide qué opciones
    /// mostrar, pero la API vuelve a validar el rol con el JWT.
    /// </summary>
    public static class RolesSistema
    {
        public const string Administrador = "Administrador";
        public const string EmpleadoVenta = "EmpleadoVenta";
        public const string Personal = Administrador + "," + EmpleadoVenta;
    }
}
