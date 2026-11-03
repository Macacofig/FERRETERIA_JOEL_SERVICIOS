namespace FERRETERIA__Joel.Aplicacion.Modelos
{
    /// <summary>
    /// Resultado de una operación sobre el personal (baja lógica o
    /// reactivación). Indica si fue exitosa y, en caso contrario, el motivo.
    /// </summary>
    public class ResultadoPersonal
    {
        /// <summary>Indica si la operación se aplicó.</summary>
        public bool Exitoso { get; init; }

        /// <summary>Mensaje para mostrar al usuario.</summary>
        public string Mensaje { get; init; } = string.Empty;

        /// <summary>Estado de la cuenta tras la operación.</summary>
        public sbyte Estado { get; init; }

        /// <summary>Identificador del empleado afectado.</summary>
        public int IdEmpleado { get; init; }
    }
}
