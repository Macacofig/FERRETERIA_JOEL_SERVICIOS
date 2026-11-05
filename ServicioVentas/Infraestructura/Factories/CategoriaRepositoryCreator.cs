using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;
using ServicioVentas.Infraestructura.Repositorios;

namespace ServicioVentas.Infraestructura.Factories
{
    public class CategoriaRepositoryCreator : RepositoryCreator<ICategoriaRepository>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CategoriaRepositoryCreator(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public override ICategoriaRepository CreateRepository()
        {
            return new MySqlCategoriaRepository(_connectionFactory);
        }
    }
}
