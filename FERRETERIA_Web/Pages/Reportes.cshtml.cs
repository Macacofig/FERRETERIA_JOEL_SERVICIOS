using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages;

[Authorize(Roles = RolesSistema.Administrador)]
public sealed class ReportesModel : PageModel
{
    private readonly ReportesApiCliente _reportes;
    private readonly ILogger<ReportesModel> _logger;

    public ReportesModel(ReportesApiCliente reportes, ILogger<ReportesModel> logger)
    {
        _reportes = reportes;
        _logger = logger;
    }

    [BindProperty] public decimal? PrecioMinimo { get; set; }
    [BindProperty] public decimal? PrecioMaximo { get; set; }
    [BindProperty] public int? CantidadVendidaMinima { get; set; }
    [BindProperty] public DateTime Desde { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    [BindProperty] public DateTime Hasta { get; set; } = DateTime.Today;
    [BindProperty] public string MetodoPago { get; set; } = "Efectivo";
    public string? Mensaje { get; private set; }
    public List<ProductoEstableApi> Productos { get; private set; } = [];
    public List<VentaProductoApi> VentasProducto { get; private set; } = [];
    public string TortaCss { get; private set; } = "conic-gradient(#4472C4 0)";

    public void OnGet() => ViewData["ActiveMenu"] = "Reportes";

    public async Task<IActionResult> OnPostConsultarProductosAsync(CancellationToken cancellationToken)
    {
        if (!ValidarProductos()) return Page();
        var token = HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Login");
        try
        {
            var resultado = await _reportes.ConsultarProductosAsync(
                token, PrecioMinimo, PrecioMaximo, CantidadVendidaMinima, cancellationToken);
            if (!resultado.Exito)
            {
                Mensaje = resultado.Mensaje ?? "La solicitud del reporte no es válida.";
                return Page();
            }
            Productos = resultado.Datos ?? [];
            Mensaje = Productos.Count == 0 ? "No se encontraron productos con los filtros indicados." : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar productos.");
            Mensaje = "No se pudo consultar el reporte de productos. Inténtelo nuevamente.";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostConsultarVentasAsync(CancellationToken cancellationToken)
    {
        if (!ValidarVentas()) return Page();
        var token = HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Login");
        try
        {
            var resultado = await _reportes.ConsultarVentasAsync(
                token, Desde, Hasta, MetodoPago, cancellationToken);
            if (!resultado.Exito)
            {
                Mensaje = resultado.Mensaje ?? "La solicitud del reporte no es válida.";
                return Page();
            }
            VentasProducto = resultado.Datos ?? [];
            Mensaje = VentasProducto.Count == 0 ? "No se encontraron ventas con los filtros indicados." : null;
            PrepararTorta();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar ventas por producto.");
            Mensaje = "No se pudo consultar el reporte de ventas. Inténtelo nuevamente.";
        }
        return Page();
    }

    public Task<IActionResult> OnPostProductosPdfAsync(CancellationToken cancellationToken)
        => DescargarAsync($"api/reportes/productos-estables/pdf{ProductosQuery}", "application/pdf",
            $"productos-stock-estable-{DateTime.Now:yyyyMMdd}.pdf", cancellationToken);

    public Task<IActionResult> OnPostProductosExcelAsync(CancellationToken cancellationToken)
        => DescargarAsync($"api/reportes/productos-estables/excel{ProductosQuery}",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"productos-stock-estable-{DateTime.Now:yyyyMMdd}.xlsx", cancellationToken);

    public Task<IActionResult> OnPostVentasPdfAsync(CancellationToken cancellationToken)
        => DescargarAsync($"api/reportes/ventas-producto/pdf{VentasQuery}", "application/pdf",
            $"ventas-producto-{DateTime.Now:yyyyMMdd}.pdf", cancellationToken);

    public Task<IActionResult> OnPostVentasExcelAsync(CancellationToken cancellationToken)
        => DescargarAsync($"api/reportes/ventas-producto/excel{VentasQuery}",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ventas-producto-{DateTime.Now:yyyyMMdd}.xlsx", cancellationToken);

    private string ProductosQuery
    {
        get
        {
            var valores = new List<string>();
            if (PrecioMinimo.HasValue) valores.Add($"precioMinimo={PrecioMinimo.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            if (PrecioMaximo.HasValue) valores.Add($"precioMaximo={PrecioMaximo.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            if (CantidadVendidaMinima.HasValue) valores.Add($"cantidadVendidaMinima={CantidadVendidaMinima.Value}");
            return valores.Count == 0 ? "" : "?" + string.Join("&", valores);
        }
    }
    private string VentasQuery =>
        $"?desde={Desde:yyyy-MM-dd}&hasta={Hasta:yyyy-MM-dd}&metodoPago={Uri.EscapeDataString(MetodoPago)}";

    private bool ValidarProductos()
    {
        if (PrecioMinimo.HasValue && PrecioMaximo.HasValue && PrecioMinimo > PrecioMaximo)
        {
            Mensaje = "El precio mínimo no puede superar el precio máximo.";
            return false;
        }
        if (CantidadVendidaMinima.HasValue && CantidadVendidaMinima < 0)
        {
            Mensaje = "La cantidad vendida mínima no puede ser negativa.";
            return false;
        }
        return true;
    }

    private bool ValidarVentas()
    {
        if (Hasta.Date < Desde.Date)
        {
            Mensaje = "La fecha hasta debe ser igual o posterior a la fecha desde.";
            return false;
        }
        return true;
    }

    private async Task<IActionResult> DescargarAsync(
        string ruta, string tipoContenido, string nombreArchivo, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString(UsuariosApiCliente.ClaveSesionJwt);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Login");
        try
        {
            var resultado = await _reportes.DescargarAsync(ruta, token, cancellationToken);
            return resultado.Exito && resultado.Datos is not null
                ? File(resultado.Datos, tipoContenido, nombreArchivo)
                : PageWithError(resultado.Mensaje ?? "No se pudo generar el reporte solicitado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descargar reporte.");
            return PageWithError("No se pudo conectar con el servicio de reportes. Inténtelo nuevamente.");
        }
    }

    private PageResult PageWithError(string mensaje)
    {
        Mensaje = mensaje;
        return Page();
    }

    private void PrepararTorta()
    {
        var total = VentasProducto.Sum(v => v.TotalProducto);
        if (total <= 0 || VentasProducto.Count == 0) return;
        var colores = new[] { "#4472C4", "#ED7D31", "#70AD47", "#FFC000", "#5B9BD5", "#A5A5A5" };
        var segmentos = new List<string>();
        decimal inicio = 0;
        for (int i = 0; i < VentasProducto.Count; i++)
        {
            var fin = inicio + VentasProducto[i].TotalProducto / total * 100;
            segmentos.Add($"{colores[i % colores.Length]} {inicio.ToString(System.Globalization.CultureInfo.InvariantCulture)}% {fin.ToString(System.Globalization.CultureInfo.InvariantCulture)}%");
            inicio = fin;
        }
        TortaCss = "conic-gradient(" + string.Join(", ", segmentos) + ")";
    }
}
