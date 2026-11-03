namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Puerto de envío de correo. La aplicación depende de esta abstracción y
    /// la infraestructura decide el transporte (SMTP real o registro en el log
    /// cuando no hay servidor configurado en desarrollo).
    /// </summary>
    public interface IEnviadorCorreo
    {
        /// <summary>
        /// Envía el correo de activación con el enlace que el empleado debe
        /// abrir para definir su contraseña.
        /// </summary>
        Task EnviarActivacionAsync(
            string destinatario,
            string nombreEmpleado,
            string enlaceActivacion);
    }
}
