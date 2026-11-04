using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Alta de personal autorizada por un administrador. Valida los datos,
    /// verifica que el correo sea único, genera el nombre de usuario, asigna el
    /// rol elegido y crea la cuenta en estado PendienteActivacion con un token
    /// de activación. La contraseña queda nula y el enlace de activación se
    /// envía por correo (válido 10 minutos).
    /// </summary>
    public class ServicioRegistroEmpleado
    {
        /// <summary>Vigencia del token de activación.</summary>
        public static readonly TimeSpan VigenciaToken =
            TimeSpan.FromMinutes(10);

        private const int MaximoSufijoNombreUsuario = 9999;

        private readonly IRepositoryEmpleado _repositorio;
        private readonly RegistroCredencialesValidador _validador;
        private readonly IEnviadorCorreo _enviadorCorreo;
        private readonly IConstructorEnlaceActivacion _constructorEnlace;
        private readonly IContextoUsuario _contextoUsuario;

        public ServicioRegistroEmpleado(
            IRepositoryEmpleado repositorio,
            RegistroCredencialesValidador validador,
            IEnviadorCorreo enviadorCorreo,
            IConstructorEnlaceActivacion constructorEnlace,
            IContextoUsuario contextoUsuario)
        {
            _repositorio = repositorio;
            _validador = validador;
            _enviadorCorreo = enviadorCorreo;
            _constructorEnlace = constructorEnlace;
            _contextoUsuario = contextoUsuario;
        }

        public async Task<ResultadoRegistro> RegistrarAsync(
            IRegistroCredenciales solicitud)
        {
            Dictionary<string, string> erroresCampo =
                _validador.Validar(solicitud);

            if (!erroresCampo.ContainsKey(
                    nameof(IRegistroCredenciales.CorreoElectronico))
                && await _repositorio.ExistePorCorreoAsync(
                    solicitud.CorreoElectronico.Trim()))
            {
                erroresCampo[
                    nameof(IRegistroCredenciales.CorreoElectronico)] =
                    "El correo ya está registrado.";
            }

            if (erroresCampo.Any())
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    ErroresCampo = erroresCampo
                };
            }

            string token = GeneradorTokenActivacion.Generar();
            string nombreUsuario =
                await GenerarNombreUsuarioDisponibleAsync(
                    solicitud.Nombre,
                    solicitud.Apellido);

            Empleado empleado = new()
            {
                Nombre = solicitud.Nombre.Trim(),
                Apellido = solicitud.Apellido.Trim(),
                NombreUsuario = nombreUsuario,
                Rol = RolesSistema.Normalizar(solicitud.Rol),
                Telefono = solicitud.Telefono.Trim(),
                CorreoElectronico = solicitud.CorreoElectronico.Trim(),
                Contrasena = null,
                TokenActivacion = token,
                TokenExpira = DateTime.Now.Add(VigenciaToken),
                TokenUsado = false,
                Estado = EstadoEmpleado.PendienteActivacion,
                FechaRegistro = DateTime.Now,
                IdEmpleadoResponsable = _contextoUsuario.IdEmpleado > 0
                    ? _contextoUsuario.IdEmpleado
                    : null,
                UsuarioCreacion = _contextoUsuario.IdEmpleado > 0
                    ? _contextoUsuario.IdEmpleado
                    : null,
                FechaCreacion = DateTime.Now
            };

            int id = await _repositorio.InsertarAsync(empleado);

            string enlace = _constructorEnlace.Construir(token);

            await _enviadorCorreo.EnviarActivacionAsync(
                empleado.CorreoElectronico!,
                $"{empleado.Nombre} {empleado.Apellido}".Trim(),
                enlace);

            return new ResultadoRegistro
            {
                Exitoso = true,
                Id = id,
                NombreUsuario = nombreUsuario,
                EnlaceActivacion = _constructorEnlace
                    .EsEnlaceDeDesarrollo
                    ? enlace
                    : null
            };
        }

        /// <summary>
        /// Reenvía el enlace de activación a un empleado pendiente. Genera un
        /// token nuevo (invalida el anterior), reinicia su vigencia y vuelve a
        /// enviar el correo.
        /// </summary>
        public async Task<ResultadoRegistro> ReenviarActivacionAsync(
            int idEmpleado)
        {
            Empleado? empleado =
                await _repositorio.ObtenerPorIdSinCredencialesAsync(idEmpleado);

            if (empleado is null)
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    Errores = new List<string> { "El empleado no existe." }
                };
            }

            if (empleado.Estado != EstadoEmpleado.PendienteActivacion)
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    Errores = new List<string>
                    {
                        "La cuenta ya está activa; no requiere activación."
                    }
                };
            }

            if (string.IsNullOrWhiteSpace(empleado.CorreoElectronico))
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    Errores = new List<string>
                    {
                        "El empleado no tiene correo registrado."
                    }
                };
            }

            string token = GeneradorTokenActivacion.Generar();

            await _repositorio.ReasignarTokenActivacionAsync(
                empleado.IdEmpleado,
                token,
                DateTime.Now.Add(VigenciaToken));

            string enlace = _constructorEnlace.Construir(token);

            await _enviadorCorreo.EnviarActivacionAsync(
                empleado.CorreoElectronico,
                $"{empleado.Nombre} {empleado.Apellido}".Trim(),
                enlace);

            return new ResultadoRegistro
            {
                Exitoso = true,
                Id = empleado.IdEmpleado,
                NombreUsuario = empleado.NombreUsuario,
                EnlaceActivacion = _constructorEnlace
                    .EsEnlaceDeDesarrollo
                    ? enlace
                    : null
            };
        }

        /// <summary>
        /// Reenvía el enlace de activación a una cuenta pendiente identificada
        /// por su correo.
        /// </summary>
        public async Task<ResultadoRegistro> ReenviarPorCorreoAsync(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    Errores = new List<string>
                    {
                        "Debe indicar un correo electrónico."
                    }
                };
            }

            Empleado? empleado =
                await _repositorio.ObtenerPorCorreoSinCredencialesAsync(
                    correo.Trim());

            if (empleado is null)
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    Errores = new List<string>
                    {
                        "No existe una cuenta con ese correo."
                    }
                };
            }

            if (empleado.Estado != EstadoEmpleado.PendienteActivacion)
            {
                return new ResultadoRegistro
                {
                    Exitoso = false,
                    Errores = new List<string>
                    {
                        "La cuenta ya está activa; no requiere activación."
                    }
                };
            }

            return await ReenviarActivacionAsync(empleado.IdEmpleado);
        }

        /// <summary>
        /// Indica si existe una cuenta con ese correo y está pendiente de
        /// activación.
        /// </summary>
        public async Task<bool> EsCuentaPendienteAsync(string? correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                return false;
            }

            Empleado? empleado =
                await _repositorio.ObtenerPorCorreoSinCredencialesAsync(
                    correo.Trim());

            return empleado is not null
                && empleado.Estado == EstadoEmpleado.PendienteActivacion;
        }

        /// <summary>
        /// Lista los empleados pendientes de activación.
        /// </summary>
        public async Task<List<Empleado>> ObtenerPendientesAsync()
        {
            return await _repositorio.ObtenerPendientesActivacionAsync();
        }

        private async Task<string> GenerarNombreUsuarioDisponibleAsync(
            string? nombre,
            string? apellido)
        {
            string baseUsuario = GeneradorNombreUsuario.Base(nombre, apellido);

            for (int sufijo = 1;
                 sufijo <= MaximoSufijoNombreUsuario;
                 sufijo++)
            {
                string candidato =
                    GeneradorNombreUsuario.ConSufijo(baseUsuario, sufijo);

                if (!await _repositorio.ExistePorNombreUsuarioAsync(candidato))
                {
                    return candidato;
                }
            }

            throw new InvalidOperationException(
                "No fue posible generar un nombre de usuario único.");
        }
    }
}
