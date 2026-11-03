using ServicioVentas.Aplicacion.Modelos;

namespace ServicioVentas.Aplicacion.Servicios
{
    /// <summary>
    /// Puerto de salida: genera el comprobante de una venta en formato PDF.
    /// La capa de aplicación solo conoce este contrato; la librería concreta
    /// vive en un adaptador y puede cambiarse sin tocar los casos de uso.
    /// </summary>
    public interface IGeneradorComprobantePdf
    {
        byte[] Generar(ComprobanteVenta comprobante);
    }
}
