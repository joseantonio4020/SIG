using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain;

namespace VisorDatosSIG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodigosFijosController : ControllerBase
{
    private readonly ICodigoFijoService _service;

    public CodigosFijosController(ICodigoFijoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<CodigoFijo>>> Listar([FromQuery] byte? estado)
    {
        var result = await _service.ObtenerTodosAsync(estado);
        return Ok(result);
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<List<CodigoFijo>>> Buscar([FromQuery] string texto, [FromQuery] string? uv = null, [FromQuery] string? mza = null, [FromQuery] string? lote = null)
    {
        var result = await _service.BuscarAsync(texto, uv, mza, lote);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CodigoFijo>> ObtenerPorId(int id)
    {
        var entidad = await _service.ObtenerPorIdAsync(id);
        if (entidad == null) return NotFound();
        return Ok(entidad);
    }
}
