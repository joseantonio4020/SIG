using Microsoft.EntityFrameworkCore;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain;
using VisorDatosSIG.Infrastructure.Persistence;

namespace VisorDatosSIG.Api.Services;

public class LoteServiceImpl : ILoteService
{
    private readonly VisorDatosSIGContext _context;

    public LoteServiceImpl(VisorDatosSIGContext context)
    {
        _context = context;
    }

    public async Task<List<Lote>> ObtenerTodosAsync()
    {
        return await _context.Lotes.ToListAsync();
    }

    public async Task<Lote?> ObtenerPorIdAsync(int id)
    {
        return await _context.Lotes.FindAsync(id);
    }

    public async Task<Lote> CrearAsync(Lote entidad)
    {
        _context.Lotes.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<Lote> ActualizarAsync(Lote entidad)
    {
        _context.Lotes.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var entidad = await _context.Lotes.FindAsync(id);
        if (entidad == null) return false;
        _context.Lotes.Remove(entidad);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task CompletarIdManzanaAsync()
    {
        await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE l 
            SET l.IdManzana = m.IdManzana
            FROM Lotes l
            INNER JOIN Manzanas m ON l.Geom.STIntersects(m.Geom) = 1
            WHERE l.IdManzana IS NULL");
    }
}
