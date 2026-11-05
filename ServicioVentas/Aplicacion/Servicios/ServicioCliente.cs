using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;
using System.Text.RegularExpressions;

namespace ServicioVentas.Aplicacion.Servicios
{
    /// <summary>
    /// Caso de uso de clientes. Permite dar de alta un cliente desde el
    /// propio flujo de venta (sin abandonar la pantalla) y consultar el
    /// catálogo de clientes activos. El cliente no tiene credenciales: el
    /// empleado en sesión queda registrado como responsable del alta.
    /// </summary>
    public class ServicioCliente
    {
        private const string MensajeRevisarDatos =
            "Revise los datos señalados e inténtelo nuevamente.";

        private readonly ClienteValidador _validador = new();
        private readonly RepositoryCreator<IClienteRepository> _creadorRepositorio;
        private readonly IContextoUsuario _contextoUsuario;
        private IClienteRepository? _repositorio;

        public ServicioCliente(
            RepositoryCreator<IClienteRepository> creadorRepositorio,
            IContextoUsuario contextoUsuario)
        {
            _creadorRepositorio = creadorRepositorio;
            _contextoUsuario = contextoUsuario;
        }

        private IClienteRepository Repositorio =>
            _repositorio ??= _creadorRepositorio.CreateRepository();

        public async Task<List<Cliente>> ListarAsync()
        {
            return await Repositorio.ObtenerTodosAsync();
        }

        public async Task<List<Cliente>> BuscarAsync(string termino)
        {
            string buscado = NormalizarTexto(termino);

            return buscado.Length == 0
                ? new List<Cliente>()
                : await Repositorio.BuscarAsync(buscado);
        }

        public async Task<Cliente?> ObtenerPorIdAsync(int idCliente)
        {
            return await Repositorio.ObtenerPorIdAsync(idCliente);
        }

        public async Task<ResultadoCliente> CrearAsync(
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

            nombre = NormalizarTexto(nombre);
            apellido = NormalizarTexto(apellido);
            ci = NormalizarCi(ci);

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

        // Quita espacios sobrantes: "  Ana   María " -> "Ana María".
        private static string NormalizarTexto(string? texto)
        {
            return Regex.Replace(texto?.Trim() ?? string.Empty, @"\s+", " ");
        }

        // Quita espacios y pasa a mayúsculas: "1234567 - 1a" -> "1234567-1A".
        private static string NormalizarCi(string? ci)
        {
            return Regex.Replace(ci ?? string.Empty, @"\s", string.Empty).ToUpperInvariant();
        }
    }
}
