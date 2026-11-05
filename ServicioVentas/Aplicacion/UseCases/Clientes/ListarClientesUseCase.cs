using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Clientes
{
    /// <summary>
    /// Caso de uso: listar el catálogo completo de clientes.
    /// </summary>
    public interface IListarClientesUseCase
    {
        Task<List<Cliente>> ListarAsync();
    }

    public class ListarClientesUseCase : IListarClientesUseCase
    {
        private readonly RepositoryCreator<IClienteRepository> _creadorRepositorio;
        private IClienteRepository? _repositorio;

        public ListarClientesUseCase(
            RepositoryCreator<IClienteRepository> creadorRepositorio)
        {
            _creadorRepositorio = creadorRepositorio;
        }

        private IClienteRepository Repositorio =>
            _repositorio ??= _creadorRepositorio.CreateRepository();

        public async Task<List<Cliente>> ListarAsync()
        {
            return await Repositorio.ObtenerTodosAsync();
        }
    }
}
