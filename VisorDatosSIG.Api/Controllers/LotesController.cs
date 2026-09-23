using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain;

namespace VisorDatosSIG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LotesController : ControllerBase
{
    private readonly ILoteService _service;

    public LotesController(ILoteService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<Lote>>> Listar()
    {
        var result = await _service.ObtenerTodosAsync();
        return Ok(result);
    }

    [HttpGet("manzana/{idManzana}")]
    public async Task<ActionResult<List<Lote>>> PorManzana(int idManzana)
    {
        var result = await _service.ObtenerTodosAsync();
        return Ok(result.Where(l => l.IdManzana == idManzana).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Lote>> ObtenerPorId(int id)
    {
        var entidad = await _service.ObtenerPorIdAsync(id);
        if (entidad == null) return NotFound();
        return Ok(entidad);
    }
}
