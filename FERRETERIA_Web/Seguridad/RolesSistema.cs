namespace FERRETERIA__Joel.Dominio.Constantes
{
    public static class RolesSistema
    {
        public const string Administrador = "Administrador";
        public const string EmpleadoVenta = "EmpleadoVenta";
        public const string Personal = Administrador + "," + EmpleadoVenta;

        public static string Normalizar(string? rol)
        {
            return string.Equals(rol?.Trim(), Administrador,
                    StringComparison.OrdinalIgnoreCase)
                ? Administrador
                : EmpleadoVenta;
        }
    }

    public static class EstadoEmpleado
    {
        public const sbyte Inactivo = 0;
        public const sbyte Activo = 1;
        public const sbyte PendienteActivacion = -1;
    }
}
