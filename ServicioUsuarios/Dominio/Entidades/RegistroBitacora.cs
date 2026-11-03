namespace FERRETERIA__Joel.Dominio.Entidades
{
    /// <summary>
    /// Fila de la bitácora. Reutiliza la tabla existente
    /// <c>bitacora_sesion</c>: <c>Accion</c> guarda tanto los eventos de sesión
    /// (login/logout) como las operaciones CRUD (INSERT/UPDATE/DELETE).
    /// </summary>
    public class RegistroBitacora
    {
        public int IdBitacora { get; set; }

        public string Accion { get; set; } = string.Empty;

        public int? IdUsuario { get; set; }

        public string? NombreUsuario { get; set; }

        public string TipoUsuario { get; set; } = string.Empty;

        public DateTime FechaHora { get; set; }

        /// <summary>Tabla o entidad afectada por la operación.</summary>
        public string? Entidad { get; set; }

        /// <summary>Identificador del registro afectado.</summary>
        public int? IdRegistroAfectado { get; set; }

        /// <summary>Resultado de la operación: EXITOSO o FALLIDO.</summary>
        public string Resultado { get; set; } = "EXITOSO";
    }
}
