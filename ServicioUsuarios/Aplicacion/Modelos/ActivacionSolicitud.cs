namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Datos del formulario de activación de cuenta: el empleado abre el
    /// enlace recibido por correo (que trae el token) y define su contraseña.
    /// </summary>
    public class ActivacionSolicitud
    {
        public string Token { get; set; } = string.Empty;

        public string Contrasena { get; set; } = string.Empty;

        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}
