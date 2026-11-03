namespace FERRETERIA__Joel.Dominio.Constantes
{
    /// <summary>
    /// Estados posibles de la cuenta de un empleado.
    ///
    /// El alta de personal no define contraseña: se crea la cuenta con
    /// <see cref="PendienteActivacion"/> y se envía un enlace de activación al
    /// correo. El empleado define su contraseña al abrir el enlace y la cuenta
    /// pasa a <see cref="Activo"/>.
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
