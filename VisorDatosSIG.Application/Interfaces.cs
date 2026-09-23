using System;
using System.Collections.Generic;
using VisorDatosSIG.Domain;

namespace VisorDatosSIG.Application.Services;

/// <summary>
/// Servicio para operaciones de códigos fijos
/// </summary>
public interface ICodigoFijoService
{
    Task<List<CodigoFijo>> ObtenerTodosAsync(byte? estado = null);
    Task<CodigoFijo?> ObtenerPorIdAsync(int id);
    Task<CodigoFijo> CrearAsync(CodigoFijo entidad);
    Task<CodigoFijo> ActualizarAsync(CodigoFijo entidad);
    Task<bool> EliminarAsync(int id);
    Task<List<CodigoFijo>> BuscarAsync(string texto, string? uv = null, string? mza = null, string? lote = null);
}

/// <summary>
/// Servicio para operaciones de manzanas
/// </summary>
public interface IManzanaService
{
    Task<List<Mzana>> ObtenerTodosAsync();
    Task<Mzana?> ObtenerPorIdAsync(int id);
    Task<Mzana> CrearAsync(Mzana entidad);
    Task<Mzana> ActualizarAsync(Mzana entidad);
    Task<bool> EliminarAsync(int id);
}

/// <summary>
/// Servicio para operaciones de lotes
/// </summary>
public interface ILoteService
{
    Task<List<Lote>> ObtenerTodosAsync();
    Task<Lote?> ObtenerPorIdAsync(int id);
    Task<Lote> CrearAsync(Lote entidad);
    Task<Lote> ActualizarAsync(Lote entidad);
    Task<bool> EliminarAsync(int id);
    Task CompletarIdManzanaAsync(); // Llena IdManzana cuando SHP no tiene vínculo
}

/// <summary>
/// Servicio para operaciones de vías
/// </summary>
public interface IViaService
{
    Task<List<Via>> ObtenerTodosAsync();
    Task<Via?> ObtenerPorIdAsync(int id);
    Task<Via> CrearAsync(Via entidad);
    Task<Via> ActualizarAsync(Via entidad);
    Task<bool> EliminarAsync(int id);
}

/// <summary>
/// Servicio de autenticación y seguridad
/// </summary>
public interface IAuthService
{
    Task<string> LoginAsync(string login, string password);
    Task<(bool Success, Usuario? Usuario, string Message)> AuthenticateAsync(string login, string password, string? ip = null);
    Task<string> RegisterAsync(string login, string nombre, string password, string rol = "Consultor");
    Task<bool> CambiarPasswordAsync(int idUsuario, string passwordActual, string nuevoPassword);
    Task<bool> ResetPasswordAsync(int idUsuario, string nuevoPassword);
    Task<Usuario?> ObtenerUsuarioActualAsync();
    Task<List<Usuario>> ObtenerTodosUsuariosAsync();
    Task<bool> ActualizarRolAsync(int idUsuario, string nuevoRol);
    Task<bool> ToggleActivoAsync(int idUsuario);
    Task RegistrarBitacoraAccesoAsync(string login, string tipoEvento, string? ip, string? detalle);
}

/// <summary>
/// Modelo de usuario para la aplicación
/// </summary>
public class UsuarioApp
{
    public int IdUsuario { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public byte[]? PasswordHash { get; set; }
    public byte[]? PasswordSalt { get; set; }
    public int Iteraciones { get; set; }
    public bool Activo { get; set; }
    public List<RolSistema> Roles { get; set; } = new();
    public List<PermisoMenu> Permisos { get; set; } = new();
}