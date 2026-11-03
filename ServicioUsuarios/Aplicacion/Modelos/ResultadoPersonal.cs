namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Resultado de una operaciÃ³n sobre el personal (baja lÃ³gica o
    /// reactivaciÃ³n). Indica si fue exitosa y, en caso contrario, el motivo.
    /// </summary>
    public class ResultadoPersonal
    {
        /// <summary>Indica si la operaciÃ³n se aplicÃ³.</summary>
        public bool Exitoso { get; init; }

        /// <summary>Mensaje para mostrar al usuario.</summary>
        public string Mensaje { get; init; } = string.Empty;

        /// <summary>Estado de la cuenta tras la operaciÃ³n.</summary>
        public sbyte Estado { get; init; }

        /// <summary>Identificador del empleado afectado.</summary>
        public int IdEmpleado { get; init; }
    }
}

