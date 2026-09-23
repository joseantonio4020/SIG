using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain;

namespace VisorDatosSIG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ManzanasController : ControllerBase
{
    private readonly IManzanaService _service;

    public ManzanasController(IManzanaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<Mzana>>> Listar()
    {
        var result = await _service.ObtenerTodosAsync();
        return Ok(result);
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<List<Mzana>>> Buscar([FromQuery] string? uv = null, [FromQuery] string? mza = null)
    {
        var result = await _service.ObtenerTodosAsync();
        if (!string.IsNullOrEmpty(uv))
            result = result.Where(m => m.UV == uv).ToList();
        if (!string.IsNullOrEmpty(mza))
            result = result.Where(m => m.MZA == mza).ToList();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Mzana>> ObtenerPorId(int id)
    {
        var entidad = await _service.ObtenerPorIdAsync(id);
        if (entidad == null) return NotFound();
        return Ok(entidad);
    }
}
