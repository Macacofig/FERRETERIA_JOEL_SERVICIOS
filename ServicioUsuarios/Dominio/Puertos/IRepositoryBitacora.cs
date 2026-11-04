using FERRETERIA__Joel.Dominio.Entidades;

namespace FERRETERIA__Joel.Dominio.Puertos
{
    /// <summary>
    /// Puerto de salida de la bitácora. La capa de Aplicación
    /// (<c>ServicioBitacora</c>) depende de esta abstracción y no del
    /// repositorio MySQL: el adaptador de Infraestructura la implementa.
    /// Así el hexágono no conoce la base de datos.
    /// </summary>
    public interface IRepositoryBitacora
    {
        /// <summary>Evento de sesión (login / logout).</summary>
        Task<int> RegistrarAsync(
            string accion,
            int? idUsuario,
            string? nombreUsuario,
            string tipoUsuario);

        /// <summary>
        /// Operación CRUD (INSERT / UPDATE / DELETE) con la entidad afectada,
        /// el registro y el resultado.
        /// </summary>
        Task<int> RegistrarOperacionAsync(
            string accion,
            string? entidad,
            int? idRegistroAfectado,
            string resultado,
            int? idUsuario,
            string? nombreUsuario,
            string tipoUsuario);

        /// <summary>Últimos registros, del más reciente al más antiguo.</summary>
        Task<List<RegistroBitacora>> ListarAsync(int limite);
    }
}
