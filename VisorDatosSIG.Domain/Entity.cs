using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using NetTopologySuite.Geometries;

namespace VisorDatosSIG.Domain;

/// <summary>
/// Entidad principal para códigos fijos / medidores
/// </summary>
public class CodigoFijo
{
    public int IdCodigo { get; set; }
    public int? CodF_SQL { get; set; }
    public string? CodF_SIG { get; set; }
    public int? CodFijo { get; set; }
    public string? Nombre { get; set; }
    public int Estado { get; set; }
    public DateTime FechaCambioEstado { get; set; }
    public int? IdLote { get; set; }
    public float? Longitud { get; set; }
    public float? Latitud { get; set; }
    public Geometry? Geom { get; set; }
}

/// <summary>
/// Entidad para manzanas (polígonos)
/// </summary>
public class Mzana
{
    public int IdManzana { get; set; }
    public int? IdOrigen { get; set; }
    public string? UV_MZA { get; set; }
    public string? UV { get; set; }
    public string? MZA { get; set; }
    public Geometry? Geom { get; set; }
}

/// <summary>
/// Entidad para lotes (polígonos)
/// </summary>
public class Lote
{
    public int IdLote { get; set; }
    public int? IdOrigen { get; set; }
    public string? NroLote { get; set; }
    public int? IdManzana { get; set; }
    public Geometry? Geom { get; set; }
    public int? IdCodigo { get; set; }
    public Mzana? Manzana { get; set; }
}

/// <summary>
/// Entidad para vías (líneas)
/// </summary>
public class Via
{
    public int IdVia { get; set; }
    public int? OBJECTID { get; set; }
    public string? Nombre { get; set; }
    public string? TipoVia { get; set; }
    public string? OSMID { get; set; }
    public Geometry? Geom { get; set; }
}

/// <summary>
/// Enumeración de estados de códigos fijos
/// </summary>
public enum EstadoCodigoFijo
{
    Normal = 1,
    ParaCorte = 2,
    Cortado = 3,
    BajaParcial = 4,
    BajaTotal = 5
}

/// <summary>
/// Enumeración de roles del sistema
/// </summary>
public enum RolSistema
{
    Administrador = 1,
    Catastro,
    Lecturador,
    Cortador,
    Reconexion
}

/// <summary>
/// Enumeración de permisos de menú
/// </summary>
public enum PermisoMenu
{
    Ver = 1,
    Crear = 2,
    Editar = 3,
    Eliminar = 4
}

/// <summary>
/// Entidad para usuarios del sistema
/// </summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public byte[]? PasswordHash { get; set; }
    public byte[]? PasswordSalt { get; set; }
    public int Iteraciones { get; set; }
    public bool Activo { get; set; }
    public string Rol { get; set; } = "Consultor";
    public DateTime FechaCreacion { get; set; }
}