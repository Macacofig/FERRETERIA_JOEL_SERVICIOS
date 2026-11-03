namespace FERRETERIA__Joel.Dominio.Puertos
{
    public interface IHasheadorContrasena
    {
        string Hashear(string contrasena);

        bool Verificar(string contrasena, string hash);
    }
}
