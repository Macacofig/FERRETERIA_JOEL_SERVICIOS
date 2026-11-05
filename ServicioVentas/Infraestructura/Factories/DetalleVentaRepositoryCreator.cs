using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using ServicioVentas.Infraestructura.Repositorios;

namespace ServicioVentas.Infraestructura.Factories
{
    public class DetalleVentaRepositoryCreator : RepositoryCreator<IDetalleVentaRepository>
    {
        private readonly IConexionTransaccional _conexion;

        public DetalleVentaRepositoryCreator(IConexionTransaccional conexion)
        {
            _conexion = conexion;
        }

        public override IDetalleVentaRepository CreateRepository()
        {
            return new MySqlDetalleVentaRepository(_conexion);
        }
    }
}
