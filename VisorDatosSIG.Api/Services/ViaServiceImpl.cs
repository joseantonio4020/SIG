using Microsoft.EntityFrameworkCore;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain;
using VisorDatosSIG.Infrastructure.Persistence;

namespace VisorDatosSIG.Api.Services;

public class ViaServiceImpl : IViaService
{
    private readonly VisorDatosSIGContext _context;

    public ViaServiceImpl(VisorDatosSIGContext context)
    {
        _context = context;
    }

    public async Task<List<Via>> ObtenerTodosAsync()
    {
        return await _context.Vias.ToListAsync();
    }

    public async Task<Via?> ObtenerPorIdAsync(int id)
    {
        return await _context.Vias.FindAsync(id);
    }

    public async Task<Via> CrearAsync(Via entidad)
    {
        _context.Vias.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<Via> ActualizarAsync(Via entidad)
    {
        _context.Vias.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var entidad = await _context.Vias.FindAsync(id);
        if (entidad == null) return false;
        _context.Vias.Remove(entidad);
        await _context.SaveChangesAsync();
        return true;
    }
}
