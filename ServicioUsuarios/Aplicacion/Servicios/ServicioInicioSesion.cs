using System.Collections.Concurrent;
using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Infraestructura.Factories;
using FERRETERIA__Joel.Dominio.Utilidades;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    // Inicio de sesión exclusivo para empleados: verifica la contraseña con
    // el hasheador, comprueba que la cuenta esté activa y normaliza el rol a
    // los roles operativos del sistema (Administrador / EmpleadoVenta).
    // Los clientes ya no inician sesión.
    public class ServicioInicioSesion
    {
        private const int IntentosPermitidos = 5;
        private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);
        private static readonly ConcurrentDictionary<string, IntentosLogin> Intentos =
            new(StringComparer.OrdinalIgnoreCase);
        private const string MensajeCredenciales =
            "Correo o contraseña incorrectos.";

        private const string MensajeFormato =
            "Ingrese un correo y una contraseña válidos.";

        private const string MensajeInactivo =
            "La cuenta está inactiva. Contacte al administrador.";

        private const string MensajePendienteActivacion =
            "La cuenta aún no está activada. Revise el enlace enviado a su correo.";

        private readonly RepositoryCreator<IRepositoryEmpleado> _creadorRepositorio;
        private readonly IHasheadorContrasena _hasheador;
        private readonly ICredencialesValidaciones _credenciales;
        private IRepositoryEmpleado? _repositorio;

        public ServicioInicioSesion(
            RepositoryCreator<IRepositoryEmpleado> creadorRepositorio,
            IHasheadorContrasena hasheador,
            ICredencialesValidaciones credenciales)
        {
            _creadorRepositorio = creadorRepositorio;
            _hasheador = hasheador;
            _credenciales = credenciales;
        }

        private IRepositoryEmpleado Repositorio =>
            _repositorio ??= _creadorRepositorio.CreateRepository();

        public async Task<ResultadoInicioSesion> IniciarAsync(
            InicioSesionSolicitud solicitud)
        {
            string correo = solicitud.CorreoElectronico?.Trim() ?? string.Empty;
            string contrasena = solicitud.Contrasena ?? string.Empty;

            if (EstaBloqueado(correo, out TimeSpan restante))
            {
                return new ResultadoInicioSesion
                {
                    Exitoso = false,
                    Mensaje = $"Demasiados intentos fallidos. Intente nuevamente en {Math.Ceiling(restante.TotalMinutes)} minutos."
                };
            }

            bool identificadorValido = correo.Contains('@')
                ? _credenciales.EsCorreoValido(correo)
                : !string.IsNullOrWhiteSpace(correo);

            if (!identificadorValido || string.IsNullOrEmpty(contrasena))
            {
                RegistrarIntentoFallido(correo);
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
                await Repositorio
                    .ObtenerPorIdentificadorConCredencialesAsync(correo);

            if (empleado is null)
            {
                RegistrarIntentoFallido(correo);
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

            bool coincide = _hasheador.Verificar(
                contrasena,
                empleado.Contrasena ?? string.Empty);
            if (!coincide)
            {
                RegistrarIntentoFallido(
                    empleado.CorreoElectronico
                    ?? empleado.NombreUsuario
                    ?? empleado.IdEmpleado.ToString());
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

            Intentos.TryRemove(
                empleado.CorreoElectronico
                ?? empleado.NombreUsuario
                ?? empleado.IdEmpleado.ToString(),
                out _);
            if (_hasheador.NecesitaRehash(empleado.Contrasena ?? string.Empty))
            {
                await Repositorio.ActualizarContrasenaAsync(
                    empleado.IdEmpleado,
                    _hasheador.Hashear(contrasena),
                    empleado.IdEmpleado);
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

        private static bool EsHashArgon2Actual(string valor) =>
            valor.StartsWith("$argon2id$", StringComparison.Ordinal);

        private static bool EstaBloqueado(string identificador, out TimeSpan restante)
        {
            restante = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(identificador)
                || !Intentos.TryGetValue(identificador, out IntentosLogin? intento))
                return false;
            if (intento.BloqueadoHasta <= DateTime.UtcNow) return false;
            restante = intento.BloqueadoHasta - DateTime.UtcNow;
            return true;
        }

        private static void RegistrarIntentoFallido(string identificador)
        {
            if (string.IsNullOrWhiteSpace(identificador)) return;
            var estado = Intentos.GetOrAdd(identificador, _ => new IntentosLogin());
            estado.Fallidos++;
            if (estado.Fallidos >= IntentosPermitidos)
            {
                estado.BloqueadoHasta = DateTime.UtcNow.Add(DuracionBloqueo);
            }
        }

        private sealed class IntentosLogin
        {
            public int Fallidos;
            public DateTime BloqueadoHasta;
        }
    }
}
