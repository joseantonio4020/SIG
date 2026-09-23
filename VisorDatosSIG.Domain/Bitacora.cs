namespace VisorDatosSIG.Domain;

/// <summary>
/// Registro de accesos al sistema (login exitoso, fallido, logout)
/// </summary>
public class BitacoraAcceso
{
    public int IdAcceso { get; set; }
    public string Login { get; set; } = string.Empty;
    public string TipoEvento { get; set; } = string.Empty; // "LOGIN_OK", "LOGIN_FAIL", "LOGOUT"
    public DateTime FechaHora { get; set; }
    public string? DireccionIP { get; set; }
    public string? Detalle { get; set; }
}

/// <summary>
/// Registro de migraciones de datos realizadas con el Migrador
/// </summary>
public class BitacoraMigracion
{
    public int Id { get; set; }
    public DateTime FechaMigracion { get; set; } = DateTime.Now;
    public string Usuario { get; set; } = string.Empty;
    public int CapasMigradas { get; set; }
    public int TotalRegistros { get; set; }
    public string Estado { get; set; } = "Completado";
    public string? Observaciones { get; set; }
}
