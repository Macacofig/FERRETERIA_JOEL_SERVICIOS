namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Puerto de salida: emisión de tokens de sesión. La tecnología concreta
    /// (JWT) es un detalle del adaptador de Infraestructura.
    /// </summary>
    public interface IEmisorToken
    {
        int DuracionMinutos { get; }

        (string Token, DateTime Expira) Emitir(
            int idEmpleado,
            string nombreCompleto,
            string correoElectronico,
            string rol,
            string? nombreUsuario);
    }
}