using FERRETERIA__Joel.Dominio.Constantes;
using FERRETERIA__Joel.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA__Joel.Pages
{
    /// <summary>
    /// Catálogo de productos de solo consulta. Los datos llegan de
    /// ServicioVentas con el JWT de la sesión; la página no consulta MySQL.
    /// </summary>
    [Authorize(Roles = RolesSistema.Personal)]
    public class CatalogoProductosModel : PageModel
    {
        private readonly VentasApiCliente _api;
        private readonly ILogger<CatalogoProductosModel> _logger;

        public CatalogoProductosModel(
            VentasApiCliente api,
            ILogger<CatalogoProductosModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public List<ProductoCatalogoApi> Productos { get; set; } = new();

        public bool SoloActivos { get; set; }

        public async Task OnGetAsync(bool soloActivos = false)
        {
            SoloActivos = soloActivos;

            try
            {
                Productos = await _api.ObtenerCatalogoAsync(
                    ObtenerToken(),
                    soloActivos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo cargar el catálogo de productos.");
                TempData["MensajeError"] =
                    "No se pudo cargar el catálogo de productos.";
                Productos = new List<ProductoCatalogoApi>();
            }
        }

        private string? ObtenerToken()
        {
            return HttpContext.Session.GetString(
                UsuariosApiCliente.ClaveSesionJwt);
        }
    }
}
