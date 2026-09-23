using Microsoft.AspNetCore.Mvc;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain;

namespace VisorDatosSIG.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ViasController : ControllerBase
{
    private readonly IViaService _service;

    public ViasController(IViaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<Via>>> Listar()
    {
        var result = await _service.ObtenerTodosAsync();
        return Ok(result);
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<List<Via>>> Buscar([FromQuery] string nombre = "", [FromQuery] string tipoVia = "")
    {
        var result = await _service.ObtenerTodosAsync();
        if (!string.IsNullOrEmpty(nombre))
            result = result.Where(v => v.Nombre!.Contains(nombre)).ToList();
        if (!string.IsNullOrEmpty(tipoVia))
            result = result.Where(v => v.TipoVia!.Contains(tipoVia)).ToList();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Via>> ObtenerPorId(int id)
    {
        var entidad = await _service.ObtenerPorIdAsync(id);
        if (entidad == null) return NotFound();
        return Ok(entidad);
    }
}
