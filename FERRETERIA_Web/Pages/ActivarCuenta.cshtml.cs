using FERRETERIA_Web.Models;
using FERRETERIA_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA_Web.Pages;

[AllowAnonymous]
public sealed class ActivarCuentaModel(IServicioUsuariosClient servicioUsuarios) : PageModel
{
    public string? Mensaje { get; private set; }
    public bool Activada { get; private set; }

    [BindProperty]
    public ActivarCuentaRequest Solicitud { get; set; } = new(string.Empty, string.Empty);

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync([FromQuery] string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Mensaje = "El enlace de activación no es válido.";
            return Page();
        }

        var resultado = await servicioUsuarios.ActivarAsync(
            token,
            Solicitud,
            cancellationToken);
        if (!resultado.Exitoso)
        {
            Mensaje = resultado.Error;
            return Page();
        }

        Activada = true;
        Mensaje = "Cuenta activada correctamente. Ya puede iniciar sesión.";
        return Page();
    }
}
