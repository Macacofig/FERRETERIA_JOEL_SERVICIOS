using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Clientes
{
    /// <summary>
    /// Caso de uso: obtener un cliente por su identificador.
    /// </summary>
    public interface IObtenerClienteUseCase
    {
        Task<Cliente?> ObtenerPorIdAsync(int idCliente);
    }

    public class ObtenerClienteUseCase : IObtenerClienteUseCase
    {
        private readonly RepositoryCreator<IClienteRepository> _creadorRepositorio;
        private IClienteRepository? _repositorio;

        public ObtenerClienteUseCase(
            RepositoryCreator<IClienteRepository> creadorRepositorio)
        {
            _creadorRepositorio = creadorRepositorio;
        }

        private IClienteRepository Repositorio =>
            _repositorio ??= _creadorRepositorio.CreateRepository();

        public async Task<Cliente?> ObtenerPorIdAsync(int idCliente)
        {
            return await Repositorio.ObtenerPorIdAsync(idCliente);
        }
    }
}
