using FERRETERIA__Joel.Dominio.Puertos;

namespace FERRETERIA__Joel.Infraestructura.Servicios
{
    /// <summary>
    /// Registro de tokens invalidados antes de su expiración natural.
    /// <para>
    /// Cubre dos casos: el cierre de sesión (se revoca el <c>jti</c> del token
    /// concreto) y la baja lógica o el cambio de contraseña (se incrementa la
    /// generación del empleado, de modo que todos los tokens emitidos antes
    /// quedan inválidos). No se guarda ningún dato sensible: solo
    /// identificadores y contadores en memoria.
    /// </para>
    /// <para>
    /// El registro vive en el proceso de la API. Tras reiniciarla, los tokens
    /// vigentes vuelven a aceptarse hasta su expiración natural; la baja se
    /// sigue bloqueando porque <c>GET /me</c> y el login releen el estado de la
    /// cuenta en MySQL.
    /// </para>
    /// </summary>
    public class RevocacionTokens : IRevocacionTokens
    {
        private readonly Dictionary<string, DateTime> _revocados = new();
        private readonly Dictionary<int, int> _generaciones = new();
        private readonly object _candado = new();

        public void Revocar(string? jti, DateTime expira)
        {
            if (string.IsNullOrWhiteSpace(jti))
            {
                return;
            }

            lock (_candado)
            {
                LimpiarVencidos();
                _revocados[jti] = expira.ToUniversalTime();
            }
        }

        public bool EstaRevocado(string? jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
            {
                return false;
            }

            lock (_candado)
            {
                LimpiarVencidos();

                return _revocados.TryGetValue(jti, out DateTime expira)
                    && expira > DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Generación actual de tokens del empleado. Los JWT incluyen este
        /// valor en el claim <c>gen</c>; al revocar se incrementa y los tokens
        /// anteriores dejan de coincidir.
        /// </summary>
        public int ObtenerGeneracion(int idEmpleado)
        {
            if (idEmpleado <= 0)
            {
                return 0;
            }

            lock (_candado)
            {
                return _generaciones.TryGetValue(idEmpleado, out int generacion)
                    ? generacion
                    : 0;
            }
        }

        /// <summary>
        /// Invalida todos los tokens emitidos hasta ahora para el empleado. Se
        /// usa en la baja lógica y al cambiar la contraseña.
        /// </summary>
        public void RevocarUsuario(int idEmpleado)
        {
            if (idEmpleado <= 0)
            {
                return;
            }

            lock (_candado)
            {
                int generacionActual = _generaciones.TryGetValue(
                    idEmpleado,
                    out int generacion)
                    ? generacion
                    : 0;

                _generaciones[idEmpleado] = generacionActual + 1;
            }
        }

        /// <summary>
        /// Indica si un token sigue perteneciendo a la generación vigente del
        /// empleado.
        /// </summary>
        public bool EstaVigenteUsuario(int idEmpleado, int generacionToken)
        {
            if (idEmpleado <= 0)
            {
                return false;
            }

            return generacionToken == ObtenerGeneracion(idEmpleado);
        }

        private void LimpiarVencidos()
        {
            DateTime ahora = DateTime.UtcNow;

            foreach (string clave in _revocados
                         .Where(par => par.Value <= ahora)
                         .Select(par => par.Key)
                         .ToList())
            {
                _revocados.Remove(clave);
            }
        }
    }
}

