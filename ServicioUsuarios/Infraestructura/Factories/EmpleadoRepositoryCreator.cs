using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Infraestructura.Conexion;
using FERRETERIA__Joel.Infraestructura.Repositorios;

namespace FERRETERIA__Joel.Infraestructura.Factories
{
    public class EmpleadoRepositoryCreator : RepositoryCreator<IRepositoryEmpleado>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public EmpleadoRepositoryCreator(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public override IRepositoryEmpleado CreateRepository()
        {
            return new MySqlEmpleadoRepository(_connectionFactory);
        }
    }
}
