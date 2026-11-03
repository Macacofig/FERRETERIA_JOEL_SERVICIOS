using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Infraestructura.Repositorios;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// BitÃ¡cora de sesiones y de operaciones CRUD. Registra el empleado
    /// autenticado (identificador, nombre y rol), la operaciÃ³n, la entidad, el
    /// registro afectado, la fecha/hora y el resultado. Nunca recibe ni guarda
    /// contraseÃ±as, hashes, JWT o tokens de activaciÃ³n.
    /// </summary>
    public class ServicioBitacora
    {
        public const string AccionLogin = "login";
        public const string AccionLogout = "logout";
        public const string OperacionInsert = "INSERT";
        public const string OperacionUpdate = "UPDATE";
        public const string OperacionDelete = "DELETE";

        private readonly MySqlBitacoraRepository _repositorio;

        public ServicioBitacora(MySqlBitacoraRepository repositorio)
        {
            _repositorio = repositorio;
        }

        public async Task RegistrarLoginAsync(
            int? idUsuario,
            string? nombreUsuario,
            string tipoUsuario)
        {
            await RegistrarAsync(
                AccionLogin,
                idUsuario,
                nombreUsuario,
                tipoUsuario);
        }

        public async Task RegistrarLogoutAsync(
            int? idUsuario,
            string? nombreUsuario,
            string tipoUsuario)
        {
            await RegistrarAsync(
                AccionLogout,
                idUsuario,
                nombreUsuario,
                tipoUsuario);
        }

        /// <summary>
        /// Registra una operaciÃ³n CRUD real. <paramref name="exitoso"/> se
        /// traduce a EXITOSO/FALLIDO. El identificador del responsable debe
        /// provenir siempre del JWT validado, nunca de un valor enviado por el
        /// cliente.
        /// </summary>
        public async Task RegistrarOperacionAsync(
            string operacion,
            string entidad,
            int? idRegistroAfectado,
            bool exitoso,
            int? idUsuario,
            string? nombreUsuario,
            string tipoUsuario)
        {
            await _repositorio.RegistrarOperacionAsync(
                operacion,
                entidad,
                idRegistroAfectado,
                exitoso ? "EXITOSO" : "FALLIDO",
                idUsuario,
                nombreUsuario,
                NormalizarRol(tipoUsuario));
        }

        public async Task<List<RegistroBitacora>> ListarAsync(int limite)
        {
            int limiteSeguro = Math.Clamp(limite, 1, 500);

            return await _repositorio.ListarAsync(limiteSeguro);
        }

        private async Task RegistrarAsync(
            string accion,
            int? idUsuario,
            string? nombreUsuario,
            string tipoUsuario)
        {
            await _repositorio.RegistrarAsync(
                accion,
                idUsuario,
                nombreUsuario,
                NormalizarRol(tipoUsuario));
        }

        private static string NormalizarRol(string? tipoUsuario)
        {
            return string.IsNullOrWhiteSpace(tipoUsuario)
                ? RolesSistema.EmpleadoVenta
                : tipoUsuario;
        }
    }
}

