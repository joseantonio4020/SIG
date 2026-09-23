using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain;

namespace VisorDatosSIG.Web.Controllers;

[ApiController]
[Route("api/administracion")]
public class AdministracionController : ControllerBase
{
    private readonly IAuthService _authService;

    public AdministracionController(IAuthService authService)
    {
        _authService = authService;
    }

    private bool IsAdmin()
    {
        var rol = HttpContext.Session.GetString("Rol");
        return rol == "Administrador";
    }

    [HttpGet("usuarios")]
    public async Task<IActionResult> GetUsuarios()
    {
        if (!IsAdmin())
            return StatusCode(403, new { error = "Acceso restringido a Administradores" });

        var usuarios = await _authService.ObtenerTodosUsuariosAsync();
        var result = usuarios.Select(u => new
        {
            u.IdUsuario,
            u.Login,
            u.Nombre,
            u.Rol,
            u.Activo,
            FechaCreacion = u.FechaCreacion.ToString("yyyy-MM-dd HH:mm")
        });

        return Ok(result);
    }

    public class CrearUsuarioDto
    {
        public string Login { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Rol { get; set; } = "Consultor";
    }

    [HttpPost("usuarios")]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto dto)
    {
        if (!IsAdmin())
            return StatusCode(403, new { error = "Acceso restringido a Administradores" });

        if (string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { error = "Todos los campos obligatorios deben completarse" });

        var res = await _authService.RegisterAsync(dto.Login.Trim(), dto.Nombre.Trim(), dto.Password, dto.Rol);
        if (res.Contains("ya se encuentra en uso"))
            return BadRequest(new { error = res });

        var adminUser = HttpContext.Session.GetString("Usuario") ?? "admin";
        await _authService.RegistrarBitacoraAccesoAsync(adminUser, "USER_CREATE", HttpContext.Connection.RemoteIpAddress?.ToString(), $"Usuario {dto.Login} creado con rol {dto.Rol}");

        return Ok(new { message = res });
    }

    public class ActualizarRolDto
    {
        public string Rol { get; set; } = string.Empty;
    }

    [HttpPut("usuarios/{id:int}/rol")]
    public async Task<IActionResult> ActualizarRol(int id, [FromBody] ActualizarRolDto dto)
    {
        if (!IsAdmin())
            return StatusCode(403, new { error = "Acceso restringido a Administradores" });

        var ok = await _authService.ActualizarRolAsync(id, dto.Rol);
        if (!ok) return NotFound(new { error = "Usuario no encontrado" });

        var adminUser = HttpContext.Session.GetString("Usuario") ?? "admin";
        await _authService.RegistrarBitacoraAccesoAsync(adminUser, "USER_ROLE_CHANGE", HttpContext.Connection.RemoteIpAddress?.ToString(), $"Rol de usuario #{id} cambiado a {dto.Rol}");

        return Ok(new { message = "Rol actualizado correctamente" });
    }

    [HttpPut("usuarios/{id:int}/toggle-activo")]
    public async Task<IActionResult> ToggleActivo(int id)
    {
        if (!IsAdmin())
            return StatusCode(403, new { error = "Acceso restringido a Administradores" });

        var ok = await _authService.ToggleActivoAsync(id);
        if (!ok) return NotFound(new { error = "Usuario no encontrado" });

        var adminUser = HttpContext.Session.GetString("Usuario") ?? "admin";
        await _authService.RegistrarBitacoraAccesoAsync(adminUser, "USER_STATUS_TOGGLE", HttpContext.Connection.RemoteIpAddress?.ToString(), $"Estado de usuario #{id} modificado");

        return Ok(new { message = "Estado de usuario cambiado exitosamente" });
    }

    public class ResetPasswordDto
    {
        public string NuevoPassword { get; set; } = string.Empty;
    }

    [HttpPost("usuarios/{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
    {
        if (!IsAdmin())
            return StatusCode(403, new { error = "Acceso restringido a Administradores" });

        if (string.IsNullOrWhiteSpace(dto.NuevoPassword) || dto.NuevoPassword.Length < 6)
            return BadRequest(new { error = "La contraseña debe tener al menos 6 caracteres" });

        var ok = await _authService.ResetPasswordAsync(id, dto.NuevoPassword);
        if (!ok) return NotFound(new { error = "Usuario no encontrado" });

        var adminUser = HttpContext.Session.GetString("Usuario") ?? "admin";
        await _authService.RegistrarBitacoraAccesoAsync(adminUser, "PASS_RESET", HttpContext.Connection.RemoteIpAddress?.ToString(), $"Contraseña de usuario #{id} restablecida");

        return Ok(new { message = "Contraseña restablecida exitosamente" });
    }
}
