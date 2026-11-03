using FERRETERIA__Joel.Aplicacion.Modelos;
using FERRETERIA__Joel.Dominio.Entidades;
using FERRETERIA__Joel.Dominio.Puertos;
using FERRETERIA__Joel.Dominio.Utilidades;

namespace FERRETERIA__Joel.Aplicacion.Servicios
{
    /// <summary>
    /// Cambio de contraseÃ±a de un empleado autenticado. Verifica la contraseÃ±a
    /// actual, aplica la polÃ­tica (mÃ­nimo 8, mayÃºscula, minÃºscula, nÃºmero y
    /// sÃ­mbolo), exige que la nueva sea distinta de la actual y guarda
    /// Ãºnicamente el hash Argon2id. La contraseÃ±a anterior deja de ser vÃ¡lida
    /// para el siguiente inicio de sesiÃ³n.
    /// </summary>
    public class ServicioCambioContrasena
    {
        private const string MensajeActualObligatoria =
            "Debe ingresar su contraseÃ±a actual.";

        private const string MensajeActualIncorrecta =
            "La contraseÃ±a actual no es correcta.";

        private const string MensajeNuevaObligatoria =
            "Debe ingresar la nueva contraseÃ±a.";

        private const string MensajeConfirmacion =
            "La confirmaciÃ³n de la nueva contraseÃ±a no coincide.";

        private const string MensajeIgual =
            "La nueva contraseÃ±a debe ser diferente de la actual.";

        private static readonly string MensajePolitica =
            "La nueva contraseÃ±a debe tener entre 8 y 64 caracteres e incluir al menos una mayÃºscula, una minÃºscula, un nÃºmero y un sÃ­mbolo.";

        private readonly IRepositoryEmpleado _repositorio;
        private readonly IHasheadorContrasena _hasheador;
        private readonly CredencialesValidaciones _validaciones = new();

        public ServicioCambioContrasena(
            IRepositoryEmpleado repositorio,
            IHasheadorContrasena hasheador)
        {
            _repositorio = repositorio;
            _hasheador = hasheador;
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
            else if (!_validaciones.EsConfirmacionValida(nueva, confirmacion))
            {
                errores[
                    nameof(CambioContrasenaSolicitud.ConfirmarContrasena)] =
                    MensajeConfirmacion;
            }

            if (errores.Any())
            {
                return new ResultadoCambioContrasena
                {
                    Exitoso = false,
                    Mensaje = "Revise los campos seÃ±alados.",
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

            if (!Coincide(actual, empleado.Contrasena))
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

            if (Coincide(nueva, empleado.Contrasena))
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
                _hasheador.Hashear(nueva));

            return ResultadoCambioContrasena.Ok(
                "ContraseÃ±a actualizada. Use la nueva contraseÃ±a en su prÃ³ximo inicio de sesiÃ³n.");
        }

        private bool Coincide(string contrasena, string? almacenada)
        {
            if (string.IsNullOrEmpty(contrasena)
                || string.IsNullOrEmpty(almacenada))
            {
                return false;
            }

            if (almacenada.StartsWith("$argon2", StringComparison.Ordinal))
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

            // Compatibilidad con las contraseÃ±as heredadas en texto plano.
            return string.Equals(almacenada, contrasena, StringComparison.Ordinal);
        }
    }
}

