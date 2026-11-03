using FERRETERIA_Web.Models;
using FERRETERIA_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FERRETERIA_Web.Pages;

[Authorize(Roles = "Administrador")]
public sealed class NuevoUsuarioModel(IServicioUsuariosClient servicioUsuarios) : PageModel
{
    [BindProperty]
    public CrearUsuarioRequest Solicitud { get; set; } = new(string.Empty, string.Empty, "EmpleadoVenta");

    public string? Mensaje { get; private set; }
    public string? Error { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Solicitud = Solicitud with
        {
            NombreCompleto = Solicitud.NombreCompleto.Trim(),
            Correo = Solicitud.Correo.Trim(),
            Rol = Solicitud.Rol == "Administrador" ? "Administrador" : "EmpleadoVenta"
        };

        var resultado = await servicioUsuarios.CrearAsync(Solicitud, cancellationToken);
        if (!resultado.Exitoso)
        {
            Error = resultado.Error;
            return Page();
        }

        Mensaje = "Usuario creado correctamente. Se envió el enlace de activación al correo.";
        Solicitud = new CrearUsuarioRequest(string.Empty, string.Empty, "EmpleadoVenta");
        return Page();
    }
}
