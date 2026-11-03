using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Aplicacion.Modelos
{
    /// <summary>
    /// Fila del listado de ventas: la venta junto con el nombre del
    /// cliente, resuelto por la fachada para que la presentación no
    /// necesite recorrer repositorios.
    /// </summary>
    public class VentaListado
    {
        public Venta Venta { get; set; } = new();

        public string NombreCliente { get; set; } = string.Empty;
    }
}
