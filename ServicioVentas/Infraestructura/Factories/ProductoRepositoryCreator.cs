using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using ServicioVentas.Infraestructura.Repositorios;

namespace ServicioVentas.Infraestructura.Factories
{
    public class ProductoRepositoryCreator : RepositoryCreator<IProductoRepository>
    {
        private readonly IConexionTransaccional _conexion;

        public ProductoRepositoryCreator(IConexionTransaccional conexion)
        {
            _conexion = conexion;
        }

        public override IProductoRepository CreateRepository()
        {
            return new MySqlProductoRepository(_conexion);
        }
    }
}
