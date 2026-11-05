using ServicioReportes.Aplicacion.Excepciones;
using ServicioReportes.Aplicacion.Modelos.Reportes;
using ServicioReportes.Aplicacion.Servicios;
using ServicioReportes.Dominio.Constantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServicioReportes.Controllers
{
    /// <summary>
    /// API de reportes. Expone consultas JSON y descargas PDF/Excel de los dos
    /// informes exigidos. Toda la lógica vive en la capa de aplicación; este
    /// controlador solo valida, traduce errores a HTTP y devuelve archivos.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RolesSistema.Personal)]
    public sealed class ReportesController : ControllerBase
    {
        private readonly FachadaReportes _fachada;
        private readonly ILogger<ReportesController> _logger;

        public ReportesController(
            FachadaReportes fachada,
            ILogger<ReportesController> logger)
        {
            _fachada = fachada;
            _logger = logger;
        }

        // ---------- Catálogo de filtros ----------

        [HttpGet("categorias")]
        public Task<IActionResult> Categorias(CancellationToken cancelacion)
        {
            return EjecutarAsync(async () =>
            {
                IReadOnlyList<CategoriaOpcion> categorias =
                    await _fachada.ObtenerCategoriasAsync(cancelacion);

                return Ok(new
                {
                    exito = true,
                    categorias = categorias.Select(categoria => new
                    {
                        categoria.IdCategoria,
                        categoria.Nombre
                    })
                });
            }, "No se pudieron cargar las categorías para los filtros.");
        }

        // ---------- Reporte detallado de ventas ----------

        [HttpGet("ventas")]
        public Task<IActionResult> Ventas(
            [FromQuery] SolicitudReporteVentas solicitud,
            CancellationToken cancelacion)
        {
            return EjecutarAsync(async () =>
            {
                ReporteVentas reporte = await _fachada.ObtenerVentasAsync(
                    solicitud, UsuarioActual(), cancelacion);

                return Ok(new
                {
                    exito = true,
                    reporte = MapearVentas(reporte)
                });
            }, "No se pudo generar el reporte detallado de ventas.");
        }

        [HttpGet("ventas/pdf")]
        public Task<IActionResult> VentasPdf(
            [FromQuery] SolicitudReporteVentas solicitud,
            CancellationToken cancelacion)
        {
            return EjecutarAsync(async () =>
            {
                ArchivoReporte archivo = await _fachada.ExportarVentasPdfAsync(
                    solicitud, UsuarioActual(), cancelacion);

                return File(
                    archivo.Contenido,
                    archivo.TipoContenido,
                    archivo.NombreArchivo);
            }, "No se pudo generar el PDF del reporte detallado de ventas.");
        }

        [HttpGet("ventas/excel")]
        public Task<IActionResult> VentasExcel(
            [FromQuery] SolicitudReporteVentas solicitud,
            CancellationToken cancelacion)
        {
            return EjecutarAsync(async () =>
            {
                ArchivoReporte archivo = await _fachada.ExportarVentasExcelAsync(
                    solicitud, UsuarioActual(), cancelacion);

                return File(
                    archivo.Contenido,
                    archivo.TipoContenido,
                    archivo.NombreArchivo);
            }, "No se pudo generar el Excel del reporte detallado de ventas.");
        }

        // ---------- Reporte estadístico ----------

        [HttpGet("estadisticas")]
        public Task<IActionResult> Estadisticas(
            [FromQuery] SolicitudReporteEstadisticas solicitud,
            CancellationToken cancelacion)
        {
            return EjecutarAsync(async () =>
            {
                ReporteEstadisticas reporte =
                    await _fachada.ObtenerEstadisticasAsync(
                        solicitud, UsuarioActual(), cancelacion);

                return Ok(new
                {
                    exito = true,
                    reporte = MapearEstadisticas(reporte)
                });
            }, "No se pudo generar el informe estadístico de ventas.");
        }

        [HttpGet("estadisticas/pdf")]
        public Task<IActionResult> EstadisticasPdf(
            [FromQuery] SolicitudReporteEstadisticas solicitud,
            CancellationToken cancelacion)
        {
            return EjecutarAsync(async () =>
            {
                ArchivoReporte archivo =
                    await _fachada.ExportarEstadisticasPdfAsync(
                        solicitud, UsuarioActual(), cancelacion);

                return File(
                    archivo.Contenido,
                    archivo.TipoContenido,
                    archivo.NombreArchivo);
            }, "No se pudo generar el PDF del informe estadístico.");
        }

        [HttpGet("estadisticas/excel")]
        public Task<IActionResult> EstadisticasExcel(
            [FromQuery] SolicitudReporteEstadisticas solicitud,
            CancellationToken cancelacion)
        {
            return EjecutarAsync(async () =>
            {
                ArchivoReporte archivo =
                    await _fachada.ExportarEstadisticasExcelAsync(
                        solicitud, UsuarioActual(), cancelacion);

                return File(
                    archivo.Contenido,
                    archivo.TipoContenido,
                    archivo.NombreArchivo);
            }, "No se pudo generar el Excel del informe estadístico.");
        }

        // ---------- Mapeo y manejo de errores ----------

        private static object MapearVentas(ReporteVentas reporte)
        {
            return new
            {
                reporte.Titulo,
                filtro = new
                {
                    reporte.Filtro.StockMinimo,
                    reporte.Filtro.PrecioMaximo,
                    reporte.Filtro.FiltroTexto
                },
                reporte.GeneradoPor,
                reporte.GeneradoPorCompleto,
                reporte.GeneradoEl,
                reporte.TotalRegistros,
                reporte.TotalStock,
                reporte.PrecioPromedio,
                lineas = reporte.Lineas.Select(linea => new
                {
                    linea.Codigo,
                    linea.Producto,
                    linea.Precio,
                    linea.Descripcion,
                    linea.Marca,
                    linea.Categoria,
                    linea.CantidadStock
                })
            };
        }

        private static object MapearEstadisticas(ReporteEstadisticas reporte)
        {
            return new
            {
                reporte.Titulo,
                filtro = new
                {
                    reporte.Filtro.CantidadMinima,
                    reporte.Filtro.MetodoPago,
                    reporte.Filtro.FiltroTexto
                },
                reporte.GeneradoPor,
                reporte.GeneradoPorCompleto,
                reporte.GeneradoEl,
                reporte.TotalProductos,
                reporte.TotalUnidades,
                reporte.TotalImporte,
                filas = reporte.FilasOrdenadas.Select(fila => new
                {
                    fila.Producto,
                    fila.CantidadTotal,
                    fila.TotalRecaudado,
                    fila.Participacion
                })
            };
        }

        private async Task<IActionResult> EjecutarAsync(
            Func<Task<IActionResult>> accion,
            string mensajeError)
        {
            try
            {
                return await accion();
            }
            catch (ReporteValidacionException ex)
            {
                return BadRequest(new { exito = false, mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{MensajeError}", mensajeError);

                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    exito = false,
                    mensaje = mensajeError
                });
            }
        }

        private string UsuarioActual()
        {
            return User.Identity?.Name ?? "Usuario del sistema";
        }
    }
}
