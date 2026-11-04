using ServicioVentas.Aplicacion.Modelos;
using ServicioVentas.Dominio.Constantes;
using ServicioVentas.Dominio.Entidades;
using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Factories;

namespace ServicioVentas.Aplicacion.Servicios
{
    /// <summary>
    /// Fachada (Facade) de las ventas YA REGISTRADAS: consulta del
    /// historial, detalle de una venta y anulación (borrado lógico).
    ///
    /// Al anular, la venta completa pasa a estado "Anulada" y se restauran
    /// las existencias consumidas: detalle_venta NO se elimina, se usa
    /// como fuente para reponer el stock producto a producto. La reposición
    /// se delega en <see cref="FachadaActualizacionStock"/> y todo ocurre
    /// dentro de una única transacción (commit/rollback). Es la fachada
    /// que consume la pantalla de Historial de ventas.
    /// </summary>
    public class FachadaAnulacion
    {
        private readonly RepositoryCreator<IVentaRepository> _creadorVentas;
        private readonly RepositoryCreator<IDetalleVentaRepository> _creadorDetalles;
        private readonly RepositoryCreator<ICategoriaRepository> _creadorCategorias;
        private readonly ServicioCliente _servicioCliente;
        private readonly IUnidadDeTrabajo _unidadDeTrabajo;
        private readonly IContextoUsuario _contextoUsuario;
        private readonly FachadaActualizacionStock _stock;

        private IVentaRepository? _ventas;
        private IDetalleVentaRepository? _detalles;
        private ICategoriaRepository? _categorias;

        public FachadaAnulacion(
            RepositoryCreator<IVentaRepository> creadorVentas,
            RepositoryCreator<IDetalleVentaRepository> creadorDetalles,
            RepositoryCreator<ICategoriaRepository> creadorCategorias,
            ServicioCliente servicioCliente,
            IUnidadDeTrabajo unidadDeTrabajo,
            IContextoUsuario contextoUsuario,
            FachadaActualizacionStock stock)
        {
            _creadorVentas = creadorVentas;
            _creadorDetalles = creadorDetalles;
            _creadorCategorias = creadorCategorias;
            _servicioCliente = servicioCliente;
            _unidadDeTrabajo = unidadDeTrabajo;
            _contextoUsuario = contextoUsuario;
            _stock = stock;
        }

        private IVentaRepository Ventas =>
            _ventas ??= _creadorVentas.CreateRepository();

        private IDetalleVentaRepository Detalles =>
            _detalles ??= _creadorDetalles.CreateRepository();

        private ICategoriaRepository Categorias =>
            _categorias ??= _creadorCategorias.CreateRepository();

        // ---------- Consulta (Select / historial) ----------

        public async Task<List<VentaListado>> ListarVentasListadoAsync()
        {
            List<Venta> ventas = await Ventas.ObtenerTodosAsync();

            Dictionary<int, string> nombresCliente = new();

            foreach (int idCliente in ventas.Select(v => v.IdCliente).Distinct())
            {
                Cliente? cliente =
                    await _servicioCliente.ObtenerPorIdAsync(idCliente);

                if (cliente is not null)
                {
                    nombresCliente[idCliente] =
                        $"{cliente.Nombre} {cliente.Apellido}".Trim();
                }
            }

            return ventas
                .Select(v => new VentaListado
                {
                    Venta = v,
                    NombreCliente = nombresCliente.TryGetValue(
                        v.IdCliente,
                        out string? nombre)
                        ? nombre
                        : "Cliente eliminado"
                })
                .ToList();
        }

        public async Task<ResultadoVenta?> ObtenerVentaAsync(int idVenta)
        {
            Venta? venta = await Ventas.ObtenerPorIdAsync(idVenta);

            if (venta is null)
            {
                return null;
            }

            return ResultadoVenta.Correcto(
                venta,
                await _stock.MapearLineasAsync(
                    await Detalles.ObtenerPorVentaAsync(idVenta)));
        }

        // ---------- Anulación (borrado lógico + reposición de stock + reversión de categorías) ----------

        public async Task<ResultadoVenta> AnularVentaAsync(int idVenta, string motivo)
        {
            if (idVenta <= 0)
            {
                return ResultadoVenta.Fallido("El identificador de la venta no es válido.");
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                return ResultadoVenta.Fallido("Debe indicar el motivo de la anulación.");
            }

            int idEmpleado = _contextoUsuario.IdEmpleado;
            if (idEmpleado <= 0)
            {
                return ResultadoVenta.Fallido("Debe iniciar sesión para anular la venta.");
            }

            Venta? venta = await Ventas.ObtenerPorIdAsync(idVenta);
            if (venta is null)
            {
                return ResultadoVenta.Fallido("La venta no existe.");
            }

            if (venta.EstadoVenta != EstadoVenta.Registrada)
            {
                return ResultadoVenta.Fallido("La venta ya ha sido anulada anteriormente.");
            }

            List<DetalleVenta> detalles = await Detalles.ObtenerPorVentaAsync(idVenta);

            await _unidadDeTrabajo.IniciarAsync();

            try
            {
                // 1. Marcar la venta como anulada en la BD
                if (!await Ventas.MarcarAnuladaAsync(idVenta, idEmpleado, motivo.Trim()))
                {
                    await _unidadDeTrabajo.RevertirAsync();
                    return ResultadoVenta.Fallido(
                        "No se pudo anular la venta; pudo haber sido anulada por otra sesión.");
                }

                // 2. Reponer existencias al inventario
                foreach (DetalleVenta detalle in detalles)
                {
                    await _stock.ReponerAsync(detalle.IdProducto, detalle.Cantidad);
                }

                // 3. PRIMERO CONFIRMAR LA ANULACIÓN Y LA REPOSICIÓN
                await _unidadDeTrabajo.ConfirmarAsync();
            }
            catch (Exception)
            {
                await _unidadDeTrabajo.RevertirAsync();
                return ResultadoVenta.Fallido("Error al procesar la anulación en la base de datos.");
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
            List<LineaVentaDetalle> lineas = await _stock.MapearLineasAsync(detalles);

            return ResultadoVenta.Correcto(actualizada, lineas);
        }
    }
}
