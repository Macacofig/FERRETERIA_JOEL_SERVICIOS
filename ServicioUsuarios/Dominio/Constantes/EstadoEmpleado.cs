namespace FERRETERIA__Joel.Dominio.Constantes
{
    /// <summary>
    /// Estados posibles de la cuenta de un empleado.
    ///
    /// El alta de personal no define contraseÃ±a: se crea la cuenta con
    /// <see cref="PendienteActivacion"/> y se envÃ­a un enlace de activaciÃ³n al
    /// correo. El empleado define su contraseÃ±a al abrir el enlace y la cuenta
    /// pasa a <see cref="Activo"/>.
    /// </summary>
    public static class EstadoEmpleado
    {
        // Cuenta creada y enlace de activaciÃ³n enviado, pero nunca activada.
        public const sbyte PendienteActivacion = -1;

        // Cuenta dada de baja por el administrador.
        public const sbyte Inactivo = 0;

        // Cuenta activada: el empleado definiÃ³ su contraseÃ±a con el enlace.
        public const sbyte Activo = 1;
    }
}

