using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Clientes
{
    /// <summary>
    /// Caso de uso: dar de alta un cliente desde el flujo de venta. Valida
    /// los datos, evita CI duplicados y registra al empleado en sesión como
    /// responsable del alta.
    /// </summary>
    public interface ICrearClienteUseCase
    {
        Task<ResultadoCliente> CrearClienteAsync(
            string ci,
            string nombre,
            string apellido);
    }

    public class CrearClienteUseCase : ICrearClienteUseCase
    {
        private const string MensajeRevisarDatos =
            "Revise los datos señalados e inténtelo nuevamente.";

        private readonly ClienteValidador _validador = new();
        private readonly RepositoryCreator<IClienteRepository> _creadorRepositorio;
        private readonly IContextoUsuario _contextoUsuario;
        private IClienteRepository? _repositorio;

        public CrearClienteUseCase(
            RepositoryCreator<IClienteRepository> creadorRepositorio,
            IContextoUsuario contextoUsuario)
        {
            _creadorRepositorio = creadorRepositorio;
            _contextoUsuario = contextoUsuario;
        }

        private IClienteRepository Repositorio =>
            _repositorio ??= _creadorRepositorio.CreateRepository();

        public async Task<ResultadoCliente> CrearClienteAsync(
            string ci,
            string nombre,
            string apellido)
        {
            int idEmpleado = _contextoUsuario.IdEmpleado;
            if (idEmpleado <= 0)
            {
                return new ResultadoCliente
                {
                    Exito = false,
                    Mensaje = "Debe iniciar sesión con un empleado válido para registrar clientes."
                };
            }

            nombre = ClienteNormalizador.Texto(nombre);
            apellido = ClienteNormalizador.Texto(apellido);
            ci = ClienteNormalizador.Ci(ci);

            Dictionary<string, string> errores =
                _validador.Validar(ci, nombre, apellido);

            if (errores.Count == 0 && await Repositorio.ExisteCiAsync(ci))
            {
                errores[ClienteValidador.CampoCi] =
                    "Ya existe un cliente registrado con ese CI.";
            }

            if (errores.Count > 0)
            {
                return new ResultadoCliente
                {
                    Exito = false,
                    Mensaje = MensajeRevisarDatos,
                    Errores = errores
                };
            }

            Cliente cliente = new()
            {
                Ci = ci,
                Nombre = nombre,
                Apellido = apellido,
                Estado = 1,
                FechaRegistro = DateTime.Now,
                IdEmpleadoResponsable = idEmpleado
            };

            int idCliente = await Repositorio.InsertarAsync(cliente);
            cliente.IdCliente = idCliente;

            return new ResultadoCliente
            {
                Exito = true,
                Mensaje = "Cliente registrado correctamente.",
                Cliente = cliente
            };
        }
    }
}
