using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.ConfiguracionValidacion;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Validaciones;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Valida las credenciales comunes al alta de personal (nombres, teléfono,
    /// correo y rol) y, por separado, la contraseña que el empleado define al
    /// activar su cuenta.
    /// </summary>
    public class RegistroCredencialesValidador
    {
        private const string MensajeNombre =
            "El nombre es obligatorio, debe tener máximo 100 caracteres y solo admite letras y espacios.";

        private const string MensajeApellido =
            "El apellido es obligatorio, debe tener máximo 100 caracteres y solo admite letras y espacios.";

        private const string MensajeTelefono =
            "El teléfono debe tener 8 dígitos y comenzar con 5, 6, 7 u 8.";

        private const string MensajeCorreo =
            "El correo debe tener un formato válido, por ejemplo correo@ejemplo.com.";

        private const string MensajeRol =
            "El rol debe ser Administrador o EmpleadoVenta.";

        private const string MensajeConfirmacion =
            "La confirmación de la contraseña no coincide.";

        private static readonly string MensajeContrasena =
            $"La contraseña debe tener entre {ConfiguracionCredenciales.ContrasenaLongitudMinima} y "
            + $"{ConfiguracionCredenciales.ContrasenaLongitudMaxima} caracteres e incluir al menos una "
            + "mayúscula, una minúscula, un número y un símbolo.";

        private readonly CredencialesValidaciones _credenciales = new();

        /// <summary>
        /// Valida los datos del alta de empleado. No valida contraseña: se
        /// define después durante la activación por correo.
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
        /// Valida la contraseña y su confirmación durante la activación.
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

