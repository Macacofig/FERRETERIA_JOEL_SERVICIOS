namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Puerto de envÃ­o de correo. La aplicaciÃ³n depende de esta abstracciÃ³n y
    /// la infraestructura decide el transporte (SMTP real o registro en el log
    /// cuando no hay servidor configurado en desarrollo).
    /// </summary>
    public interface IEnviadorCorreo
    {
        /// <summary>
        /// EnvÃ­a el correo de activaciÃ³n con el enlace que el empleado debe
        /// abrir para definir su contraseÃ±a.
        /// </summary>
        Task EnviarActivacionAsync(
            string destinatario,
            string nombreEmpleado,
            string enlaceActivacion);
    }
}

