using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Utilidades;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Valida las credenciales comunes al alta de personal (nombres, telÃ©fono,
    /// correo y rol) y, por separado, la contraseÃ±a que el empleado define al
    /// activar su cuenta.
    /// </summary>
    public class RegistroCredencialesValidador
    {
        private const string MensajeNombre =
            "El nombre es obligatorio, debe tener mÃ¡ximo 100 caracteres y solo admite letras y espacios.";

        private const string MensajeApellido =
            "El apellido es obligatorio, debe tener mÃ¡ximo 100 caracteres y solo admite letras y espacios.";

        private const string MensajeTelefono =
            "El telÃ©fono debe tener 8 dÃ­gitos y comenzar con 5, 6, 7 u 8.";

        private const string MensajeCorreo =
            "El correo debe tener un formato vÃ¡lido, por ejemplo correo@ejemplo.com.";

        private const string MensajeRol =
            "El rol debe ser Administrador o EmpleadoVenta.";

        private const string MensajeConfirmacion =
            "La confirmaciÃ³n de la contraseÃ±a no coincide.";

        private static readonly string MensajeContrasena =
            $"La contraseÃ±a debe tener entre {ConfiguracionCredenciales.ContrasenaLongitudMinima} y "
            + $"{ConfiguracionCredenciales.ContrasenaLongitudMaxima} caracteres e incluir al menos una "
            + "mayÃºscula, una minÃºscula, un nÃºmero y un sÃ­mbolo.";

        private readonly CredencialesValidaciones _credenciales = new();

        /// <summary>
        /// Valida los datos del alta de empleado. No valida contraseÃ±a: se
        /// define despuÃ©s durante la activaciÃ³n por correo.
        /// </summary>
        public Dictionary<string, string> Validar(IRegistroCredenciales solicitud)
        {
            Dictionary<string, string> errores = new();

            if (!_credenciales.EsNombreValido(solicitud.Nombre))
            {
                errores[nameof(IRegistroCredenciales.Nombre)] = MensajeNombre;
            }

            if (!_credenciales.EsApellidoValido(solicitud.Apellido))
            {
                errores[nameof(IRegistroCredenciales.Apellido)] = MensajeApellido;
            }

            if (!_credenciales.EsTelefonoValido(solicitud.Telefono))
            {
                errores[nameof(IRegistroCredenciales.Telefono)] = MensajeTelefono;
            }

            if (!_credenciales.EsCorreoValido(solicitud.CorreoElectronico))
            {
                errores[nameof(IRegistroCredenciales.CorreoElectronico)] = MensajeCorreo;
            }

            if (!EsRolValido(solicitud.Rol))
            {
                errores[nameof(IRegistroCredenciales.Rol)] = MensajeRol;
            }

            return errores;
        }

        /// <summary>
        /// Valida la contraseÃ±a y su confirmaciÃ³n durante la activaciÃ³n.
        /// </summary>
        public Dictionary<string, string> ValidarContrasenaActivacion(
            string? contrasena,
            string? confirmarContrasena)
        {
            Dictionary<string, string> errores = new();

            if (!_credenciales.EsContrasenaValida(contrasena))
            {
                errores[nameof(ActivacionSolicitud.Contrasena)] = MensajeContrasena;
            }
            else if (!_credenciales.EsConfirmacionValida(
                contrasena,
                confirmarContrasena))
            {
                errores[nameof(ActivacionSolicitud.ConfirmarContrasena)] =
                    MensajeConfirmacion;
            }

            return errores;
        }

        private static bool EsRolValido(string? rol)
        {
            return string.Equals(
                    rol?.Trim(),
                    RolesSistema.Administrador,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    rol?.Trim(),
                    RolesSistema.EmpleadoVenta,
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}


