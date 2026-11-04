using ServicioVentas.Dominio.Entidades;

namespace ServicioVentas.Dominio.Puertos
{
    /// <summary>
    /// Contrato de solo lectura de empleados usado para resolver el nombre
    /// del responsable y del usuario que anula en el comprobante de venta.
    /// </summary>
    public interface IEmpleadoRepository : IRepository<Empleado>
    {
    }
}
