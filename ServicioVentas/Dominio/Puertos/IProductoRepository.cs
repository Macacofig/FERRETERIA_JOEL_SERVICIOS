using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Dominio.Puertos
{
    /// <summary>
    /// Contrato de lectura del catálogo de productos que consume el módulo
    /// de ventas. La escritura del catálogo pertenece al módulo de productos.
    /// </summary>
    public interface IProductoRepository : IGestionarRepository<Producto>
    {
        Task<Producto?> ObtenerPorIdAsync(int idProducto);
    }
}
