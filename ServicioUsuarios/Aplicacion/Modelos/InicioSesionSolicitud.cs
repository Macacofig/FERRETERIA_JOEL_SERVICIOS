namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Credenciales ingresadas en el formulario de inicio de sesión de empleado.
    /// </summary>
    public class InicioSesionSolicitud
    {
        public string CorreoElectronico { get; set; } = string.Empty;

        public string Contrasena { get; set; } = string.Empty;
    }
}
