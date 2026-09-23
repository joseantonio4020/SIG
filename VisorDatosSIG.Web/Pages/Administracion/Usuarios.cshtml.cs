using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VisorDatosSIG.Web.Pages.Administracion;

public class UsuariosModel : PageModel
{
    public IActionResult OnGet()
    {
        var rol = HttpContext.Session.GetString("Rol");
        if (rol != "Administrador")
        {
            return RedirectToPage("/Inicio");
        }
        return Page();
    }
}
