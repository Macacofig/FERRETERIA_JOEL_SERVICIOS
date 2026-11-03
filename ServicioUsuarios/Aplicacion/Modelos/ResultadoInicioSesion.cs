namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Resultado del inicio de sesión: indica si fue exitoso, el mensaje a
    /// mostrar y, cuando corresponde, la identidad autenticada.
    /// </summary>
    public class ResultadoInicioSesion
    {
        public bool Exitoso { get; init; }

        public string Mensaje { get; init; } = string.Empty;

        public SesionUsuario? Usuario { get; init; }
    }
}
