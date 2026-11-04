namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Datos del formulario de cambio de contraseña de un empleado ya
    /// autenticado.
    /// </summary>
    public class CambioContrasenaSolicitud
    {
        public string ContrasenaActual { get; set; } = string.Empty;

        public string ContrasenaNueva { get; set; } = string.Empty;

        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}

