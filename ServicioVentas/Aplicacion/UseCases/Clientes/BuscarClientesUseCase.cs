using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Clientes
{
    /// <summary>
    /// Caso de uso: buscar clientes por CI, nombre o apellido.
    /// </summary>
    public interface IBuscarClientesUseCase
    {
        Task<List<Cliente>> BuscarClientesAsync(string termino);
    }

    public class BuscarClientesUseCase : IBuscarClientesUseCase
    {
        private readonly RepositoryCreator<IClienteRepository> _creadorRepositorio;
        private IClienteRepository? _repositorio;

        public BuscarClientesUseCase(
            RepositoryCreator<IClienteRepository> creadorRepositorio)
        {
            _creadorRepositorio = creadorRepositorio;
        }

        private IClienteRepository Repositorio =>
            _repositorio ??= _creadorRepositorio.CreateRepository();

        public async Task<List<Cliente>> BuscarClientesAsync(string termino)
        {
            string buscado = ClienteNormalizador.Texto(termino);

            return buscado.Length == 0
                ? new List<Cliente>()
                : await Repositorio.BuscarAsync(buscado);
        }
    }
}
