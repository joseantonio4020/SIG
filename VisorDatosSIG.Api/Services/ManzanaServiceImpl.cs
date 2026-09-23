using Microsoft.EntityFrameworkCore;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain;
using VisorDatosSIG.Infrastructure.Persistence;

namespace VisorDatosSIG.Api.Services;

public class ManzanaServiceImpl : IManzanaService
{
    private readonly VisorDatosSIGContext _context;

    public ManzanaServiceImpl(VisorDatosSIGContext context)
    {
        _context = context;
    }

    public async Task<List<Mzana>> ObtenerTodosAsync()
    {
        return await _context.Manzanas.ToListAsync();
    }

    public async Task<Mzana?> ObtenerPorIdAsync(int id)
    {
        return await _context.Manzanas.FindAsync(id);
    }

    public async Task<Mzana> CrearAsync(Mzana entidad)
    {
        _context.Manzanas.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<Mzana> ActualizarAsync(Mzana entidad)
    {
        _context.Manzanas.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var entidad = await _context.Manzanas.FindAsync(id);
        if (entidad == null) return false;
        _context.Manzanas.Remove(entidad);
        await _context.SaveChangesAsync();
        return true;
    }
}
