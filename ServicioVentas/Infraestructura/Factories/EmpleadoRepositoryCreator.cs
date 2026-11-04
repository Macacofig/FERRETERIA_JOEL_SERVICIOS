using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using ServicioVentas.Infraestructura.Repositorios;

namespace ServicioVentas.Infraestructura.Factories
{
    public class EmpleadoRepositoryCreator : RepositoryCreator<IEmpleadoRepository>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public EmpleadoRepositoryCreator(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public override IEmpleadoRepository CreateRepository()
        {
            return new MySqlEmpleadoRepository(_connectionFactory);
        }
    }
}
