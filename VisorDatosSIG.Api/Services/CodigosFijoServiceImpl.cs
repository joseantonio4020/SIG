using Microsoft.EntityFrameworkCore;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain;
using VisorDatosSIG.Infrastructure.Persistence;

namespace VisorDatosSIG.Api.Services;

public class CodigosFijoServiceImpl : ICodigoFijoService
{
    private readonly VisorDatosSIGContext _context;

    public CodigosFijoServiceImpl(VisorDatosSIGContext context)
    {
        _context = context;
    }

    public async Task<List<CodigoFijo>> ObtenerTodosAsync(byte? estado = null)
    {
        var query = _context.CodigosFijos.AsQueryable();
        if (estado.HasValue)
            query = query.Where(c => c.Estado == estado.Value);
        return await query.ToListAsync();
    }

    public async Task<CodigoFijo?> ObtenerPorIdAsync(int id)
    {
        return await _context.CodigosFijos.FindAsync(id);
    }

    public async Task<CodigoFijo> CrearAsync(CodigoFijo entidad)
    {
        _context.CodigosFijos.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<CodigoFijo> ActualizarAsync(CodigoFijo entidad)
    {
        _context.CodigosFijos.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var entidad = await _context.CodigosFijos.FindAsync(id);
        if (entidad == null) return false;
        _context.CodigosFijos.Remove(entidad);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<CodigoFijo>> BuscarAsync(string texto, string? uv = null, string? mza = null, string? lote = null)
    {
        var query = _context.CodigosFijos.AsQueryable();
        
        if (!string.IsNullOrEmpty(texto))
            query = query.Where(c => c.Nombre!.Contains(texto) || c.CodF_SIG!.Contains(texto));
        
        return await query.ToListAsync();
    }
}
