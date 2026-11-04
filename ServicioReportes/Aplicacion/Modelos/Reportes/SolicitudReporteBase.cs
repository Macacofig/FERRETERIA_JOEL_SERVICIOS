using ServicioReportes.Aplicacion.Excepciones;
using ServicioReportes.Dominio.Constantes;

namespace ServicioReportes.Aplicacion.Modelos.Reportes
{
    /// <summary>
    /// Filtros comunes a los dos reportes. Se enlazan desde el query string
    /// y se normalizan antes de llegar al repositorio.
    /// </summary>
    public abstract class SolicitudReporteBase
    {
        public DateTime? Desde { get; set; }

        public DateTime? Hasta { get; set; }

        public int? IdCategoria { get; set; }

        public string? EstadoVenta { get; set; }

        protected (DateTime Desde, DateTime Hasta) NormalizarRango()
        {
            DateTime hasta = (Hasta ?? DateTime.Today).Date;
            DateTime desde = (Desde ?? hasta.AddDays(-30)).Date;

            if (desde > hasta)
            {
                throw new ReporteValidacionException(
                    "La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.");
            }

            return (desde, hasta);
        }

        protected int? NormalizarCategoria()
        {
            if (IdCategoria is null or 0)
            {
                return null;
            }

            if (IdCategoria < 0)
            {
                throw new ReporteValidacionException(
                    "La categoría seleccionada no es válida.");
            }

            return IdCategoria;
        }

        protected string? NormalizarEstadoVenta()
        {
            try
            {
                return Dominio.Constantes.EstadoVenta.Normalizar(EstadoVenta);
            }
            catch (ArgumentException ex)
            {
                throw new ReporteValidacionException(ex.Message);
            }
        }
    }
}
