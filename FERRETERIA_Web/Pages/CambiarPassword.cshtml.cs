using FERRETERIA_Web.Models;
using FERRETERIA_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA_Web.Pages;

[Authorize]
public sealed class CambiarPasswordModel(IServicioUsuariosClient servicioUsuarios) : PageModel
{
    [BindProperty]
    public CambiarPasswordRequest Solicitud { get; set; } =
        new(string.Empty, string.Empty, string.Empty);

    public string? Mensaje { get; private set; }
    public string? Error { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var resultado = await servicioUsuarios.CambiarPasswordAsync(Solicitud, cancellationToken);
        if (!resultado.Exitoso)
        {
            Error = resultado.Error;
            return Page();
        }

        Mensaje = "Contraseña cambiada correctamente.";
        Solicitud = new CambiarPasswordRequest(string.Empty, string.Empty, string.Empty);
        return Page();
    }
}
