using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Infraestructura.Conexion;
using FERRETERIA__Joel.Infraestructura.Repositorios;

namespace FERRETERIA__Joel.Infraestructura.Factories
{
    public class BitacoraRepositoryCreator : RepositoryCreator<IRepositoryBitacora>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BitacoraRepositoryCreator(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public override IRepositoryBitacora CreateRepository()
        {
            return new MySqlBitacoraRepository(_connectionFactory);
        }
    }
}
