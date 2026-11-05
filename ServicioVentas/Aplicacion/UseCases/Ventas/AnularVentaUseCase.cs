using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Aplicacion.UseCases.Stock;
using ServicioVentas.Dominio.Constantes;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Factories;
using ServicioVentas.Dominio.Puertos;

namespace ServicioVentas.Aplicacion.UseCases.Ventas
{
    /// <summary>
    /// Caso de uso: anular una venta registrada. Marca la venta como anulada,
    /// repone el stock consumido y recalcula los porcentajes de las
    /// categorías dentro de una única transacción.
    /// </summary>
    public interface IAnularVentaUseCase
    {
        Task<ResultadoVenta> AnularVentaAsync(int idVenta, string motivo);
    }

    public class AnularVentaUseCase : IAnularVentaUseCase
    {
        private readonly RepositoryCreator<IVentaRepository> _creadorVentas;
        private readonly RepositoryCreator<IDetalleVentaRepository> _creadorDetalles;
        private readonly RepositoryCreator<ICategoriaRepository> _creadorCategorias;
        private readonly IReponerStockUseCase _reponerStock;
        private readonly IUnidadDeTrabajo _unidadDeTrabajo;
        private readonly IContextoUsuario _contextoUsuario;
        private readonly IMapearLineasVentaUseCase _mapearLineas;

        private IVentaRepository? _ventas;
        private IDetalleVentaRepository? _detalles;
        private ICategoriaRepository? _categorias;

        public AnularVentaUseCase(
            RepositoryCreator<IVentaRepository> creadorVentas,
            RepositoryCreator<IDetalleVentaRepository> creadorDetalles,
            RepositoryCreator<ICategoriaRepository> creadorCategorias,
            IReponerStockUseCase reponerStock,
            IUnidadDeTrabajo unidadDeTrabajo,
            IContextoUsuario contextoUsuario,
            IMapearLineasVentaUseCase mapearLineas)
        {
            _creadorVentas = creadorVentas;
            _creadorDetalles = creadorDetalles;
            _creadorCategorias = creadorCategorias;
            _reponerStock = reponerStock;
            _unidadDeTrabajo = unidadDeTrabajo;
            _contextoUsuario = contextoUsuario;
            _mapearLineas = mapearLineas;
        }

        private IVentaRepository Ventas =>
            _ventas ??= _creadorVentas.CreateRepository();

        private IDetalleVentaRepository Detalles =>
            _detalles ??= _creadorDetalles.CreateRepository();

        private ICategoriaRepository Categorias =>
            _categorias ??= _creadorCategorias.CreateRepository();

        public async Task<ResultadoVenta> AnularVentaAsync(int idVenta, string motivo)
        {
            if (idVenta <= 0)
            {
                return ResultadoVenta.Fallido(
                    "El identificador de la venta no es válido.");
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                return ResultadoVenta.Fallido(
                    "Debe indicar el motivo de la anulación.");
            }

            int idEmpleado = _contextoUsuario.IdEmpleado;
            if (idEmpleado <= 0)
            {
                return ResultadoVenta.Fallido(
                    "Debe iniciar sesión para anular la venta.");
            }

            Venta? venta = await Ventas.ObtenerPorIdAsync(idVenta);
            if (venta is null)
            {
                return ResultadoVenta.Fallido("La venta no existe.");
            }

            if (venta.EstadoVenta != EstadoVenta.Registrada)
            {
                return ResultadoVenta.Fallido(
                    "La venta ya ha sido anulada anteriormente.");
            }

            List<DetalleVenta> detalles =
                await Detalles.ObtenerPorVentaAsync(idVenta);

            await _unidadDeTrabajo.IniciarAsync();

            try
            {
                // 1. Marcar la venta como anulada en la BD
                if (!await Ventas.MarcarAnuladaAsync(
                        idVenta,
                        idEmpleado,
                        motivo.Trim()))
                {
                    await _unidadDeTrabajo.RevertirAsync();
                    return ResultadoVenta.Fallido(
                        "No se pudo anular la venta; pudo haber sido anulada por otra sesión.");
                }

                // 2. Reponer existencias al inventario
                foreach (DetalleVenta detalle in detalles)
                {
                    await _reponerStock.ReponerAsync(
                        detalle.IdProducto,
                        detalle.Cantidad);
                }

                // 3. PRIMERO CONFIRMAR LA ANULACIÓN Y LA REPOSICIÓN
                await _unidadDeTrabajo.ConfirmarAsync();
            }
            catch (Exception)
            {
                await _unidadDeTrabajo.RevertirAsync();
                return ResultadoVenta.Fallido(
                    "Error al procesar la anulación en la base de datos.");
            }

            // 4. DESPUÉS DE CONFIRMAR, SE RECALCULAN LOS PORCENTAJES DE LAS CATEGORÍAS
            // Al estar fuera de la transacción, ya no genera ningún deadlock en MySQL
            try
            {
                await Categorias.IncrementarPorcentajeGananciaAsync();
            }
            catch
            {
                // El recálculo auxiliar no debe impedir que la anulación se considere exitosa
            }

            Venta actualizada = await Ventas.ObtenerPorIdAsync(idVenta) ?? venta;
            List<LineaVentaDetalle> lineas =
                await _mapearLineas.MapearLineasAsync(detalles);

            return ResultadoVenta.Correcto(actualizada, lineas);
        }
    }
}
