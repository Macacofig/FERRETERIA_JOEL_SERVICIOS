using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Utilidades;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    // Inicio de sesiÃ³n exclusivo para empleados: verifica la contraseÃ±a con
    // el hasheador, comprueba que la cuenta estÃ© activa y normaliza el rol a
    // los roles operativos del sistema (Administrador / EmpleadoVenta).
    // Los clientes ya no inician sesiÃ³n.
    public class ServicioInicioSesion
    {
        private const string MensajeCredenciales =
            "Correo o contraseÃ±a incorrectos.";

        private const string MensajeFormato =
            "Ingrese un correo y una contraseÃ±a vÃ¡lidos.";

        private const string MensajeInactivo =
            "La cuenta estÃ¡ inactiva. Contacte al administrador.";

        private const string MensajePendienteActivacion =
            "La cuenta aÃºn no estÃ¡ activada. Revise el enlace enviado a su correo.";

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

            // Ãšnica operaciÃ³n que recupera el hash de la contraseÃ±a: el
            // resto de lecturas del personal se hacen con la proyecciÃ³n
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

        // Verifica la contraseÃ±a. Detecta contraseÃ±as heredadas guardadas en
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

