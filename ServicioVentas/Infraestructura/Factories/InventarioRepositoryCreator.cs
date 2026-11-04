using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using ServicioVentas.Infraestructura.Repositorios;

namespace ServicioVentas.Infraestructura.Factories
{
    public class InventarioRepositoryCreator : RepositoryCreator<IInventarioRepository>
    {
        private readonly IConexionTransaccional _conexion;

        public InventarioRepositoryCreator(IConexionTransaccional conexion)
        {
            _conexion = conexion;
        }

        public override IInventarioRepository CreateRepository()
        {
            return new MySqlInventarioRepository(_conexion);
        }
    }
}
