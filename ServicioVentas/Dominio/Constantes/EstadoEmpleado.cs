namespace ServicioVentas.Dominio.Constantes
{
    /// <summary>
    /// Estados posibles de la cuenta de un empleado.
    /// </summary>
    public static class EstadoEmpleado
    {
        // Cuenta creada y enlace de activación enviado, pero nunca activada.
        public const sbyte PendienteActivacion = -1;

        // Cuenta dada de baja por el administrador.
        public const sbyte Inactivo = 0;

        // Cuenta activada: el empleado definió su contraseña con el enlace.
        public const sbyte Activo = 1;
    }
}
