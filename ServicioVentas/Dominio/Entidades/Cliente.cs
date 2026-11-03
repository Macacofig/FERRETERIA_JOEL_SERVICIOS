namespace ServicioVentas.Dominio.Entidades
{
    /// <summary>
    /// Cliente de la ferretería. Se registra dentro del flujo de venta:
    /// no inicia sesión ni conserva credenciales. El empleado que lo dio de
    /// alta queda registrado en <see cref="IdEmpleadoResponsable"/> para
    /// auditoría.
    /// </summary>
    public class Cliente
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Ci { get; set; } = string.Empty;
        public byte Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public int? IdEmpleadoResponsable { get; set; }
    }
}
