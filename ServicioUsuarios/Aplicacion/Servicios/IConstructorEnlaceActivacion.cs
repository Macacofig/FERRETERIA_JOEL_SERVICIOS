namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Construye el enlace de activación que viaja en el correo. La capa Web
    /// aporta la implementación porque conoce la URL base del sitio.
    /// </summary>
    public interface IConstructorEnlaceActivacion
    {
        /// <summary>
        /// Arma la URL absoluta de la página de activación para el token dado.
        /// </summary>
        string Construir(string token);

        /// <summary>
        /// Indica si el enlace se generó en un entorno de desarrollo (sin
        /// SMTP real configurado), en cuyo caso el alta expone el enlace en la
        /// pantalla para poder probar el flujo.
        /// </summary>
        bool EsEnlaceDeDesarrollo { get; }
    }
}
