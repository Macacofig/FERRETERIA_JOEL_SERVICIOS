namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Datos del formulario de cambio de contraseÃ±a de un empleado ya
    /// autenticado.
    /// </summary>
    public class CambioContrasenaSolicitud
    {
        public string ContrasenaActual { get; set; } = string.Empty;

        public string ContrasenaNueva { get; set; } = string.Empty;

        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}

