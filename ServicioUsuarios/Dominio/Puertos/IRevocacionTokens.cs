namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Puerto de salida: control de tokens revocados y de la generación de
    /// sesión de cada usuario.
    /// </summary>
    public interface IRevocacionTokens
    {
        void Revocar(string? jti, DateTime expira);

        bool EstaRevocado(string? jti);

        int ObtenerGeneracion(int idEmpleado);

        void RevocarUsuario(int idEmpleado);

        bool EstaVigenteUsuario(int idEmpleado, int generacionToken);
    }
}