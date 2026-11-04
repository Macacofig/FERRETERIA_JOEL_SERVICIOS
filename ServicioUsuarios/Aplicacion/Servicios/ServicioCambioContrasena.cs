using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Utilidades;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Cambio de contraseña de un empleado autenticado. Verifica la contraseña
    /// actual, aplica la política (mínimo 8, mayúscula, minúscula, número y
    /// símbolo), exige que la nueva sea distinta de la actual y guarda
    /// únicamente el hash Argon2id. La contraseña anterior deja de ser válida
    /// para el siguiente inicio de sesión.
    /// </summary>
    public class ServicioCambioContrasena
    {
        private const string MensajeActualObligatoria =
            "Debe ingresar su contraseña actual.";

        private const string MensajeActualIncorrecta =
            "La contraseña actual no es correcta.";

        private const string MensajeNuevaObligatoria =
            "Debe ingresar la nueva contraseña.";

        private const string MensajeConfirmacion =
            "La confirmación de la nueva contraseña no coincide.";

        private const string MensajeIgual =
            "La nueva contraseña debe ser diferente de la actual.";

        private static readonly string MensajePolitica =
            "La nueva contraseña debe tener entre 8 y 64 caracteres e incluir al menos una mayúscula, una minúscula, un número y un símbolo.";

        private readonly IRepositoryEmpleado _repositorio;
        private readonly IHasheadorContrasena _hasheador;
        private readonly ICredencialesValidaciones _validaciones;
        private readonly IContextoUsuario _contextoUsuario;

        public ServicioCambioContrasena(
            IRepositoryEmpleado repositorio,
            IHasheadorContrasena hasheador,
            ICredencialesValidaciones validaciones,
            IContextoUsuario contextoUsuario)
        {
            _repositorio = repositorio;
            _hasheador = hasheador;
            _validaciones = validaciones;
            _contextoUsuario = contextoUsuario;
        }

        public async Task<ResultadoCambioContrasena> CambiarAsync(
            int idEmpleado,
            CambioContrasenaSolicitud solicitud)
        {
            Dictionary<string, string> errores = new();

            string actual = solicitud.ContrasenaActual ?? string.Empty;
            string nueva = solicitud.ContrasenaNueva ?? string.Empty;
            string confirmacion = solicitud.ConfirmarContrasena ?? string.Empty;

            if (string.IsNullOrWhiteSpace(actual))
            {
                errores[nameof(CambioContrasenaSolicitud.ContrasenaActual)] =
                    MensajeActualObligatoria;
            }

            if (string.IsNullOrWhiteSpace(nueva))
            {
                errores[nameof(CambioContrasenaSolicitud.ContrasenaNueva)] =
                    MensajeNuevaObligatoria;
            }
            else if (!_validaciones.EsContrasenaValida(nueva))
            {
                errores[nameof(CambioContrasenaSolicitud.ContrasenaNueva)] =
                    MensajePolitica;
            }
            
            if (!string.IsNullOrWhiteSpace(nueva)
                && !_validaciones.EsConfirmacionValida(nueva, confirmacion))
            {
                errores[nameof(CambioContrasenaSolicitud.ConfirmarContrasena)] =
                    MensajeConfirmacion;
            }

            if (errores.Any())
            {
                return new ResultadoCambioContrasena
                {
                    Exitoso = false,
                    Mensaje = "Revise los campos señalados.",
                    ErroresCampo = errores
                };
            }

            Empleado? empleado =
                await _repositorio.ObtenerPorIdConCredencialesAsync(idEmpleado);

            if (empleado is null)
            {
                return ResultadoCambioContrasena.Error(
                    "La cuenta no existe.");
            }

            if (!_hasheador.Verificar(actual, empleado.Contrasena ?? string.Empty))
            {
                return new ResultadoCambioContrasena
                {
                    Exitoso = false,
                    Mensaje = MensajeActualIncorrecta,
                    ErroresCampo = new Dictionary<string, string>
                    {
                        [nameof(CambioContrasenaSolicitud.ContrasenaActual)] =
                            MensajeActualIncorrecta
                    }
                };
            }

            if (_hasheador.Verificar(nueva, empleado.Contrasena ?? string.Empty))
            {
                return new ResultadoCambioContrasena
                {
                    Exitoso = false,
                    Mensaje = MensajeIgual,
                    ErroresCampo = new Dictionary<string, string>
                    {
                        [nameof(CambioContrasenaSolicitud.ContrasenaNueva)] =
                            MensajeIgual
                    }
                };
            }

            await _repositorio.ActualizarContrasenaAsync(
                idEmpleado,
                _hasheador.Hashear(nueva),
                _contextoUsuario.IdEmpleado > 0
                    ? _contextoUsuario.IdEmpleado
                    : idEmpleado);

            return ResultadoCambioContrasena.Ok(
                "Contraseña actualizada. Use la nueva contraseña en su próximo inicio de sesión.");
        }

    }
}
