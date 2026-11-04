namespace FERRETERIA__Joel.Servicios;

public sealed class FiltroConsultaReporte
{
    public int? StockMinimo { get; set; }
    public decimal? PrecioMaximo { get; set; }
    public int? CantidadMinima { get; set; }
    public string? MetodoPago { get; set; }
}

public sealed class CategoriaReporteApi
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class RespuestaCategoriasApi
{
    public bool Exito { get; set; }
    public string? Mensaje { get; set; }
    public List<CategoriaReporteApi> Categorias { get; set; } = new();
}

public sealed class FiltroReporteApi
{
    public int StockMinimo { get; set; }
    public decimal PrecioMaximo { get; set; }
    public int CantidadMinima { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string FiltroTexto { get; set; } = string.Empty;
}

public sealed class LineaReporteVentasApi
{
    public string Codigo { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int CantidadStock { get; set; }
}

public sealed class ReporteVentasApi
{
    public string Titulo { get; set; } = string.Empty;
    public FiltroReporteApi Filtro { get; set; } = new();
    public string GeneradoPor { get; set; } = string.Empty;
    public string GeneradoPorCompleto { get; set; } = string.Empty;
    public DateTime GeneradoEl { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalStock { get; set; }
    public decimal PrecioPromedio { get; set; }
    public List<LineaReporteVentasApi> Lineas { get; set; } = new();
}

public sealed class RespuestaReporteVentasApi
{
    public bool Exito { get; set; }
    public string? Mensaje { get; set; }
    public ReporteVentasApi? Reporte { get; set; }
}

public sealed class LineaEstadisticaApi
{
    public string Producto { get; set; } = string.Empty;
    public int CantidadTotal { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal Participacion { get; set; }
}

public sealed class ReporteEstadisticasApi
{
    public string Titulo { get; set; } = string.Empty;
    public FiltroReporteApi Filtro { get; set; } = new();
    public string GeneradoPor { get; set; } = string.Empty;
    public string GeneradoPorCompleto { get; set; } = string.Empty;
    public DateTime GeneradoEl { get; set; }
    public int TotalProductos { get; set; }
    public int TotalUnidades { get; set; }
    public decimal TotalImporte { get; set; }
    public List<LineaEstadisticaApi> Filas { get; set; } = new();
}

public sealed class RespuestaReporteEstadisticasApi
{
    public bool Exito { get; set; }
    public string? Mensaje { get; set; }
    public ReporteEstadisticasApi? Reporte { get; set; }
}

public sealed record ArchivoDescargaApi(byte[] Contenido, string NombreArchivo, string TipoContenido);

public sealed class ReportesApiException : Exception
{
    public ReportesApiException(int estado, string mensaje) : base(mensaje) => Estado = estado;
    public int Estado { get; }
}
