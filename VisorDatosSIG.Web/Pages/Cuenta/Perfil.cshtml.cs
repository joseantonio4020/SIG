using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VisorDatosSIG.Application.Services;

namespace VisorDatosSIG.Web.Pages.Cuenta;

public class PerfilModel : PageModel
{
    private readonly IAuthService _authService;

    public PerfilModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public string PasswordActual { get; set; } = string.Empty;

    [BindProperty]
    public string NuevoPassword { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmarPassword { get; set; } = string.Empty;

    public string? MensajeExito { get; set; }
    public string? MensajeError { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostCambiarPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(PasswordActual) || string.IsNullOrWhiteSpace(NuevoPassword))
        {
            MensajeError = "Debe ingresar la contraseña actual y la nueva contraseña.";
            return Page();
        }

        if (NuevoPassword != ConfirmarPassword)
        {
            MensajeError = "La nueva contraseña y su confirmación no coinciden.";
            return Page();
        }

        if (NuevoPassword.Length < 6)
        {
            MensajeError = "La nueva contraseña debe tener al menos 6 caracteres.";
            return Page();
        }

        var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 1;
        var ok = await _authService.CambiarPasswordAsync(idUsuario, PasswordActual, NuevoPassword);

        if (ok)
        {
            MensajeExito = "¡Su contraseña ha sido actualizada exitosamente!";
        }
        else
        {
            MensajeError = "La contraseña actual no es correcta.";
        }

        return Page();
    }
}
