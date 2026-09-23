using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.IO;
using VisorDatosSIG.Domain;

namespace VisorDatosSIG.Infrastructure.Persistence;

/// <summary>
/// Contexto de base de datos para VisorDatosSIG
/// </summary>
public class VisorDatosSIGContext : DbContext
{
    public VisorDatosSIGContext(DbContextOptions<VisorDatosSIGContext> options)
        : base(options)
    {
    }

    public DbSet<CodigoFijo> CodigosFijos { get; set; }
    public DbSet<Mzana> Manzanas { get; set; }
    public DbSet<Lote> Lotes { get; set; }
    public DbSet<Via> Vias { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<BitacoraAcceso> BitacoraAccesos { get; set; }
    public DbSet<BitacoraMigracion> BitacoraMigraciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de CodigosFijos
        modelBuilder.Entity<CodigoFijo>(entity =>
        {
            entity.ToTable("CodigosFijos");
            entity.HasKey(e => e.CodF_SQL);
            
            entity.Ignore(e => e.IdCodigo);
            entity.Ignore(e => e.IdLote);

            entity.Property(e => e.CodF_SQL).HasColumnName("CodF_SQL");
            entity.Property(e => e.CodF_SIG).HasColumnName("CodF_SIG").HasMaxLength(25);
            entity.Property(e => e.CodFijo).HasColumnName("CodFijo");
            entity.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(120);
            entity.Property(e => e.Estado).HasColumnName("Estado");
            entity.Property(e => e.FechaCambioEstado).HasColumnName("FechaCambioEstado");
            entity.Property(e => e.Longitud).HasColumnName("Longitud").HasColumnType("float");
            entity.Property(e => e.Latitud).HasColumnName("Latitud").HasColumnType("float");
            entity.Property(e => e.Geom).HasColumnName("Geom").HasColumnType("geometry");
        });

        // Configuración de Manzanas
        modelBuilder.Entity<Mzana>(entity =>
        {
            entity.ToTable("Manzanas");
            entity.HasKey(e => e.IdOrigen);
            
            entity.Ignore(e => e.IdManzana);

            entity.Property(e => e.IdOrigen).HasColumnName("IdOrigen");
            entity.Property(e => e.UV_MZA).HasColumnName("UV_MZA").HasMaxLength(20);
            entity.Property(e => e.UV).HasColumnName("UV").HasMaxLength(15);
            entity.Property(e => e.MZA).HasColumnName("MZA").HasMaxLength(10);
            entity.Property(e => e.Geom).HasColumnName("Geom").HasColumnType("geometry");
        });

        // Configuración de Lotes
        modelBuilder.Entity<Lote>(entity =>
        {
            entity.ToTable("Lotes");
            entity.HasKey(e => e.IdOrigen);
            
            entity.Ignore(e => e.IdLote);
            entity.Ignore(e => e.IdManzana);
            entity.Ignore(e => e.IdCodigo);
            entity.Ignore(e => e.Manzana);

            entity.Property(e => e.IdOrigen).HasColumnName("IdOrigen");
            entity.Property(e => e.NroLote).HasColumnName("NroLote").HasMaxLength(15);
            entity.Property(e => e.Geom).HasColumnName("Geom").HasColumnType("geometry");
        });

        // Configuración de Vías
        modelBuilder.Entity<Via>(entity =>
        {
            entity.ToTable("Vias");
            entity.HasKey(e => e.OBJECTID);
            
            entity.Ignore(e => e.IdVia);

            entity.Property(e => e.OBJECTID).HasColumnName("OBJECTID");
            entity.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(40);
            entity.Property(e => e.TipoVia).HasColumnName("TipoVia").HasMaxLength(30);
            entity.Property(e => e.OSMID).HasColumnName("OSMID").HasMaxLength(20);
            entity.Property(e => e.Geom).HasColumnName("Geom").HasColumnType("geometry");
        });

        // Configuración de Usuarios
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(e => e.IdUsuario);
            entity.Property(e => e.Login).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Nombre).HasMaxLength(120).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnType("varbinary(max)");
            entity.Property(e => e.PasswordSalt).HasColumnType("varbinary(max)");
            entity.HasIndex(e => e.Login).IsUnique();
        });

        // Configuración de BitacoraAcceso
        modelBuilder.Entity<BitacoraAcceso>(entity =>
        {
            entity.ToTable("BitacoraAccesos");
            entity.HasKey(e => e.IdAcceso);
            entity.Property(e => e.Login).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TipoEvento).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DireccionIP).HasMaxLength(50);
            entity.Property(e => e.Detalle).HasMaxLength(500);
        });

        // Configuración de BitacoraMigracion
        modelBuilder.Entity<BitacoraMigracion>(entity =>
        {
            entity.ToTable("BitacoraMigracion");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.FechaMigracion).HasColumnName("FechaMigracion");
            entity.Property(e => e.Usuario).HasColumnName("Usuario").HasMaxLength(100);
            entity.Property(e => e.CapasMigradas).HasColumnName("CapasMigradas");
            entity.Property(e => e.TotalRegistros).HasColumnName("TotalRegistros");
            entity.Property(e => e.Estado).HasColumnName("Estado").HasMaxLength(20);
            entity.Property(e => e.Observaciones).HasColumnName("Observaciones").HasMaxLength(500);
        });
    }
}