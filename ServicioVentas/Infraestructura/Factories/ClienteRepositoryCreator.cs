using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using ServicioVentas.Infraestructura.Repositorios;

namespace ServicioVentas.Infraestructura.Factories
{
    public class ClienteRepositoryCreator : RepositoryCreator<IClienteRepository>
    {
        private readonly IConexionTransaccional _conexion;

        public ClienteRepositoryCreator(IConexionTransaccional conexion)
        {
            _conexion = conexion;
        }

        public override IClienteRepository CreateRepository()
        {
            return new MySqlClienteRepository(_conexion);
        }
    }
}
