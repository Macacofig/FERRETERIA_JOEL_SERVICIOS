using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using ServicioVentas.Infraestructura.Repositorios;

namespace ServicioVentas.Infraestructura.Factories
{
    public class VentaRepositoryCreator : RepositoryCreator<IVentaRepository>
    {
        private readonly IConexionTransaccional _conexion;

        public VentaRepositoryCreator(IConexionTransaccional conexion)
        {
            _conexion = conexion;
        }

        public override IVentaRepository CreateRepository()
        {
            return new MySqlVentaRepository(_conexion);
        }
    }
}
