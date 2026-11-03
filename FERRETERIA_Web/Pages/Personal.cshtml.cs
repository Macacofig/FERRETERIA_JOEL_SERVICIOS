using FERRETERIA_Web.Models;
using FERRETERIA_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA_Web.Pages;

[Authorize(Roles = "Administrador")]
public sealed class PersonalModel(IServicioUsuariosClient servicioUsuarios) : PageModel
{
    public IReadOnlyList<UsuarioResponse> Personal { get; private set; } = [];
    public string? Mensaje { get; private set; }
    public string? Error { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await CargarAsync(cancellationToken);
        if (TempData["Mensaje"] is string mensaje)
            Mensaje = mensaje;
        if (TempData["Error"] is string error)
            Error = error;
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(long id, CancellationToken cancellationToken)
    {
        var resultado = await servicioUsuarios.DarDeBajaAsync(id, cancellationToken);
        if (resultado.Exitoso)
            TempData["Mensaje"] = "Usuario dado de baja correctamente.";
        else
            TempData["Error"] = resultado.Error;
        return RedirectToPage();
    }

    private async Task CargarAsync(CancellationToken cancellationToken)
    {
        try
        {
            Personal = await servicioUsuarios.ListarAsync(cancellationToken);
        }
        catch
        {
            Error = "No se pudo cargar el listado del personal.";
        }
    }
}
