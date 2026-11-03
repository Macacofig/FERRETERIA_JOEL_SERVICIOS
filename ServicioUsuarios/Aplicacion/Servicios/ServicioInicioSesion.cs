using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Validaciones;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    // Inicio de sesión exclusivo para empleados: verifica la contraseña con
    // el hasheador, comprueba que la cuenta esté activa y normaliza el rol a
    // los roles operativos del sistema (Administrador / EmpleadoVenta).
    // Los clientes ya no inician sesión.
    public class ServicioInicioSesion
    {
        private const string MensajeCredenciales =
            "Correo o contraseña incorrectos.";

        private const string MensajeFormato =
            "Ingrese un correo y una contraseña válidos.";

        private const string MensajeInactivo =
            "La cuenta está inactiva. Contacte al administrador.";

        private const string MensajePendienteActivacion =
            "La cuenta aún no está activada. Revise el enlace enviado a su correo.";

        private readonly IRepositoryEmpleado _repositorioEmpleado;
        private readonly IHasheadorContrasena _hasheador;
        private readonly CredencialesValidaciones _credenciales = new();

        public ServicioInicioSesion(
            IRepositoryEmpleado repositorioEmpleado,
            IHasheadorContrasena hasheador)
        {
            _repositorioEmpleado = repositorioEmpleado;
            _hasheador = hasheador;
        }

        public async Task<ResultadoInicioSesion> IniciarAsync(
            InicioSesionSolicitud solicitud)
        {
            string correo = solicitud.CorreoElectronico?.Trim() ?? string.Empty;
            string contrasena = solicitud.Contrasena ?? string.Empty;

            bool identificadorValido = correo.Contains('@')
                ? _credenciales.EsCorreoValido(correo)
                : !string.IsNullOrWhiteSpace(correo);

            if (!identificadorValido || string.IsNullOrEmpty(contrasena))
            {
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = MensajeFormato
                };
            }

            // Única operación que recupera el hash de la contraseña: el
            // resto de lecturas del personal se hacen con la proyección
            // sin credenciales. El identificador puede ser el correo o el
            // nombre de usuario generado en el alta.
            Empleado? empleado =
                await _repositorioEmpleado
                    .ObtenerPorIdentificadorConCredencialesAsync(correo);

            if (empleado is null)
            {
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = MensajeCredenciales
                };
            }

            return await AutenticarEmpleado(empleado, contrasena);
        }

        private async Task<ResultadoInicioSesion> AutenticarEmpleado(
            Empleado empleado,
            string contrasena)
        {
            if (empleado.Estado == EstadoEmpleado.PendienteActivacion)
            {
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = MensajePendienteActivacion
                };
            }

            if (!Coincide(
                    contrasena,
                    empleado.Contrasena ?? string.Empty,
                    out bool rehash))
            {
                return CredencialesInvalidas();
            }

            if (empleado.Estado != EstadoEmpleado.Activo)
            {
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = MensajeInactivo
                };
            }

            if (rehash)
            {
                await _repositorioEmpleado.ActualizarContrasenaAsync(
                    empleado.IdEmpleado,
                    _hasheador.Hashear(contrasena));
            }

            return new ResultadoInicioSesion
            {
                Exitoso = true,
                Usuario = new SesionUsuario
                {
                    Id = empleado.IdEmpleado,
                    NombreCompleto =
                        $"{empleado.Nombre} {empleado.Apellido}".Trim(),
                    NombreUsuario = empleado.NombreUsuario,
                    CorreoElectronico =
                        empleado.CorreoElectronico ?? string.Empty,
                    Rol = RolesSistema.Normalizar(empleado.Rol)
                }
            };
        }

        private static ResultadoInicioSesion CredencialesInvalidas()
        {
            return new ResultadoInicioSesion
            {
                Exitoso = false,
                Mensaje = MensajeCredenciales
            };
        }

        // Verifica la contraseña. Detecta contraseñas heredadas guardadas en
        // texto plano (semillas) y marca que deben re-hashearse.
        private bool Coincide(
            string contrasena,
            string almacenada,
            out bool rehash)
        {
            rehash = false;

            if (string.IsNullOrEmpty(almacenada))
            {
                return false;
            }

            if (EsHashArgon2(almacenada))
            {
                try
                {
                    return _hasheador.Verificar(contrasena, almacenada);
                }
                catch
                {
                    return false;
                }
            }

            if (string.Equals(almacenada, contrasena, StringComparison.Ordinal))
            {
                rehash = true;
                return true;
            }

            return false;
        }

        private static bool EsHashArgon2(string valor)
        {
            return valor.StartsWith("$argon2", StringComparison.Ordinal);
        }
    }
}
