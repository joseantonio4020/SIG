using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.IO;
using NetTopologySuite.Geometries;
using VisorDatosSIG.Domain;
using VisorDatosSIG.Infrastructure.Persistence;

namespace VisorDatosSIG.Web.Controllers;

[ApiController]
[Route("api/geosql")]
public class GeoSqlController : ControllerBase
{
    private readonly VisorDatosSIGContext? _context;
    private readonly GeometryFactory _geometryFactory;

    public GeoSqlController(VisorDatosSIGContext context)
    {
        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        _context = context;
    }

    [HttpGet("codigos-fijos")]
    public async Task<IActionResult> GetCodigosFijos()
    {
        if (_context == null) return Ok(GetMockCodigosFijos());
        try
        {
            var items = await _context.CodigosFijos.AsNoTracking().ToListAsync();
            var result = items.Select(c => MapCodigoFijo(c)).ToList();
            if (result.Count == 0) return Ok(GetMockCodigosFijos());
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error GetCodigosFijos]: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("codigos-fijos/buscar")]
    public async Task<IActionResult> BuscarCodigosFijos([FromQuery] string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return Ok(new List<object>());

        if (_context == null)
        {
            var mockFiltrado = GetMockCodigosFijos()
                .Where(c => ((string)c.GetType().GetProperty("nombre")!.GetValue(c)!).Contains(texto, StringComparison.OrdinalIgnoreCase)
                         || ((string)c.GetType().GetProperty("codFSig")!.GetValue(c)!).Contains(texto, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Ok(mockFiltrado);
        }

        try
        {
            var items = await _context.CodigosFijos
                .AsNoTracking()
                .Where(c => (c.Nombre != null && c.Nombre.Contains(texto)) ||
                           (c.CodF_SIG != null && c.CodF_SIG.Contains(texto)) ||
                           (c.CodFijo != null && c.CodFijo.ToString().Contains(texto)))
                .ToListAsync();
            return Ok(items.Select(c => MapCodigoFijo(c)).ToList());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error BuscarCodigosFijos]: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("manzanas")]
    public async Task<IActionResult> GetManzanas()
    {
        if (_context == null) return Ok(GetMockManzanas());
        try
        {
            var items = await _context.Manzanas.AsNoTracking().ToListAsync();
            var result = items.Select(m => new
            {
                idManzana = m.IdOrigen,
                m.UV,
                m.MZA,
                geom = m.Geom != null ? SerializeGeoJson(m.Geom) : null
            }).ToList();
            if (result.Count == 0) return Ok(GetMockManzanas());
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error GetManzanas]: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("lotes")]
    public async Task<IActionResult> GetLotes([FromQuery] double? minLon = null, [FromQuery] double? minLat = null, [FromQuery] double? maxLon = null, [FromQuery] double? maxLat = null)
    {
        if (_context == null) return Ok(GetMockLotes());
        try
        {
            var query = _context.Lotes.AsNoTracking().Where(l => l.Geom != null);

            if (minLon.HasValue && minLat.HasValue && maxLon.HasValue && maxLat.HasValue)
            {
                var envelope = _geometryFactory.CreatePolygon(new Coordinate[]
                {
                    new(minLon.Value, minLat.Value),
                    new(maxLon.Value, minLat.Value),
                    new(maxLon.Value, maxLat.Value),
                    new(minLon.Value, maxLat.Value),
                    new(minLon.Value, minLat.Value)
                });
                query = query.Where(l => l.Geom!.Intersects(envelope));
            }

            var items = await query.Take(4000).ToListAsync();
            var result = items.Select(l => new
            {
                idLote = l.IdOrigen,
                l.NroLote,
                geom = l.Geom != null ? SerializeGeoJson(l.Geom) : null
            }).ToList();
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error GetLotes]: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("vias")]
    public async Task<IActionResult> GetVias()
    {
        if (_context == null) return Ok(GetMockVias());
        try
        {
            var items = await _context.Vias.AsNoTracking().ToListAsync();
            var result = items.Select(v => new
            {
                idVia = v.OBJECTID,
                v.Nombre,
                v.TipoVia,
                geom = v.Geom != null ? SerializeGeoJson(v.Geom) : null
            }).ToList();
            if (result.Count == 0) return Ok(GetMockVias());
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error GetVias]: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        if (_context == null) return Ok(new { totalCodigos = 100, totalManzanas = 24, totalLotes = 60, totalVias = 10 });
        try
        {
            var totalCodigos = await _context.CodigosFijos.CountAsync();
            var totalManzanas = await _context.Manzanas.CountAsync();
            var totalLotes = await _context.Lotes.CountAsync();
            var totalVias = await _context.Vias.CountAsync();

            var estados = await _context.CodigosFijos
                .GroupBy(c => c.Estado)
                .Select(g => new { estado = g.Key, count = g.Count() })
                .ToListAsync();

            var ultimosAccesos = await _context.BitacoraAccesos
                .OrderByDescending(b => b.FechaHora)
                .Take(5)
                .Select(b => new { b.Login, b.TipoEvento, b.FechaHora, b.DireccionIP, b.Detalle })
                .ToListAsync();

            return Ok(new
            {
                totalCodigos,
                totalManzanas,
                totalLotes,
                totalVias,
                estados = estados.ToDictionary(k => k.estado.ToString(), v => v.count),
                ultimosAccesos,
                fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error GetStats]: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("zonas-jerarquia")]
    public async Task<IActionResult> GetZonasJerarquia([FromQuery] string? uv = null, [FromQuery] string? mza = null)
    {
        if (_context == null) return Ok(new { uvs = new List<string>(), mzas = new List<string>(), lotes = new List<string>() });
        try
        {
            var codigos = await _context.CodigosFijos
                .AsNoTracking()
                .Where(c => c.CodF_SIG != null && (c.CodF_SIG.Contains(".") || c.CodF_SIG.Contains("-")))
                .Select(c => c.CodF_SIG!)
                .ToListAsync();

            var parsed = codigos
                .Select(sig => sig.Split(new[] { '.', '-' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(parts => parts.Length >= 1)
                .Select(parts => new
                {
                    UV = parts[0].Trim(),
                    MZA = parts.Length > 1 ? parts[1].Trim() : "0",
                    Lote = parts.Length > 2 ? parts[2].Trim() : "0"
                })
                .ToList();

            var allUvs = parsed
                .Select(p => p.UV)
                .Distinct()
                .OrderBy(u => int.TryParse(u, out var n) ? n : 999999)
                .ThenBy(u => u)
                .ToList();

            List<string> mzas = new();
            List<string> lotes = new();

            if (!string.IsNullOrWhiteSpace(uv))
            {
                var filteredByUV = parsed.Where(p => p.UV.Equals(uv.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                mzas = filteredByUV
                    .Select(p => p.MZA)
                    .Distinct()
                    .OrderBy(m => int.TryParse(m, out var n) ? n : 999999)
                    .ThenBy(m => m)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(mza))
                {
                    lotes = filteredByUV
                        .Where(p => p.MZA.Equals(mza.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Select(p => p.Lote)
                        .Distinct()
                        .OrderBy(l => int.TryParse(l, out var n) ? n : 999999)
                        .ThenBy(l => l)
                        .ToList();
                }
            }

            return Ok(new
            {
                uvs = allUvs,
                mzas,
                lotes
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("codigos-fijos/consultar")]
    public async Task<IActionResult> ConsultarCodigosFijos(
        [FromQuery] string? texto = null,
        [FromQuery] string? uv = null,
        [FromQuery] string? mza = null,
        [FromQuery] string? lote = null,
        [FromQuery] int? estado = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (_context == null) return Ok(new { total = 0, page, pageSize, items = new List<object>() });
        try
        {
            var query = _context.CodigosFijos.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim();
                query = query.Where(c => (c.Nombre != null && c.Nombre.Contains(t)) ||
                                         (c.CodF_SIG != null && c.CodF_SIG.Contains(t)) ||
                                         (c.CodFijo != null && c.CodFijo.ToString().Contains(t)));
            }

            if (estado.HasValue && estado.Value > 0)
            {
                query = query.Where(c => c.Estado == estado.Value);
            }

            if (!string.IsNullOrWhiteSpace(uv))
            {
                var uvVal = uv.Trim();
                query = query.Where(c => c.CodF_SIG != null && (c.CodF_SIG.StartsWith(uvVal + ".") || c.CodF_SIG.StartsWith(uvVal + "-")));
            }

            if (!string.IsNullOrWhiteSpace(mza))
            {
                var mzaVal = mza.Trim();
                query = query.Where(c => c.CodF_SIG != null && (c.CodF_SIG.Contains("." + mzaVal + ".") || c.CodF_SIG.Contains("-" + mzaVal + "-")));
            }

            if (!string.IsNullOrWhiteSpace(lote))
            {
                var loteVal = lote.Trim();
                query = query.Where(c => c.CodF_SIG != null && (c.CodF_SIG.EndsWith("." + loteVal) || c.CodF_SIG.EndsWith("-" + loteVal)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.CodF_SQL)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mapped = items.Select(c => MapCodigoFijo(c)).ToList();

            return Ok(new { total, page, pageSize, items = mapped });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error ConsultarCodigosFijos]: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("codigos-fijos/export-csv")]
    public async Task<IActionResult> ExportarCodigosFijosCsv(
        [FromQuery] string? texto = null,
        [FromQuery] string? uv = null,
        [FromQuery] string? mza = null,
        [FromQuery] string? lote = null,
        [FromQuery] int? estado = null)
    {
        if (_context == null) return BadRequest("Sin conexión a base de datos");
        try
        {
            var query = _context.CodigosFijos.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim();
                query = query.Where(c => (c.Nombre != null && c.Nombre.Contains(t)) ||
                                         (c.CodF_SIG != null && c.CodF_SIG.Contains(t)) ||
                                         (c.CodFijo != null && c.CodFijo.ToString().Contains(t)));
            }

            if (estado.HasValue && estado.Value > 0)
                query = query.Where(c => c.Estado == estado.Value);

            if (!string.IsNullOrWhiteSpace(uv))
            {
                var uvVal = uv.Trim();
                query = query.Where(c => c.CodF_SIG != null && (c.CodF_SIG.StartsWith(uvVal + ".") || c.CodF_SIG.StartsWith(uvVal + "-")));
            }

            if (!string.IsNullOrWhiteSpace(mza))
            {
                var mzaVal = mza.Trim();
                query = query.Where(c => c.CodF_SIG != null && (c.CodF_SIG.Contains("." + mzaVal + ".") || c.CodF_SIG.Contains("-" + mzaVal + "-")));
            }

            if (!string.IsNullOrWhiteSpace(lote))
            {
                var loteVal = lote.Trim();
                query = query.Where(c => c.CodF_SIG != null && (c.CodF_SIG.EndsWith("." + loteVal) || c.CodF_SIG.EndsWith("-" + loteVal)));
            }

            var list = await query.Take(10000).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("CodF_SQL;CodF_SIG;CodFijo;Nombre;Estado;EstadoNombre;UV;MZA;LOTE;Latitud;Longitud;FechaCambioEstado");

            foreach (var item in list)
            {
                var parts = item.CodF_SIG?.Split(new[] { '.', '-' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                string uvVal = parts.Length > 0 ? parts[0].Trim() : "0";
                string mzaVal = parts.Length > 1 ? parts[1].Trim() : "0";
                string loteVal = parts.Length > 2 ? parts[2].Trim() : "0";

                string estNombre = item.Estado switch
                {
                    1 => "Normal",
                    2 => "Para corte",
                    3 => "Cortado",
                    4 => "Baja parcial",
                    5 => "Baja total",
                    _ => "Desconocido"
                };

                csv.AppendLine($"\"{item.CodF_SQL}\";\"{item.CodF_SIG}\";\"{item.CodFijo}\";\"{item.Nombre?.Replace("\"", "\"\"")}\";\"{item.Estado}\";\"{estNombre}\";\"{uvVal}\";\"{mzaVal}\";\"{loteVal}\";\"{item.Latitud}\";\"{item.Longitud}\";\"{item.FechaCambioEstado:yyyy-MM-dd HH:mm}\"");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"Reporte_CodigosFijos_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error ExportarCodigosFijosCsv]: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("bitacora/accesos")]
    public async Task<IActionResult> GetBitacoraAccesos()
    {
        if (_context == null) return Ok(new List<object>());
        try
        {
            var logs = await _context.BitacoraAccesos
                .AsNoTracking()
                .OrderByDescending(b => b.FechaHora)
                .Take(100)
                .ToListAsync();
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("bitacora/migraciones")]
    public async Task<IActionResult> GetBitacoraMigraciones()
    {
        if (_context == null) return Ok(new List<object>());
        try
        {
            var logs = await _context.BitacoraMigraciones
                .AsNoTracking()
                .OrderByDescending(b => b.FechaMigracion)
                .Take(100)
                .ToListAsync();
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ====== MAP ======
    private static object MapCodigoFijo(CodigoFijo c)
    {
        var parts = c.CodF_SIG?.Split(new[] { '.', '-' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        string uv = parts.Length > 0 ? parts[0].Trim() : "0";
        string mza = parts.Length > 1 ? parts[1].Trim() : "0";
        string lote = parts.Length > 2 ? parts[2].Trim() : "0";

        return new
        {
            idCodigo = c.CodF_SQL,
            c.CodF_SQL,
            c.CodF_SIG,
            c.CodFijo,
            c.Nombre,
            c.Estado,
            c.FechaCambioEstado,
            c.Longitud,
            c.Latitud,
            uv,
            mza,
            lote
        };
    }

    private static string? SerializeGeoJson(Geometry geom)
    {
        if (geom == null) return null;
        var writer = new GeoJsonWriter();
        var geoJson = writer.Write(geom);
        return geoJson;
    }

    // ====== MOCK DATA - Santa Cruz de la Sierra, Bolivia ======
    private List<object> GetMockCodigosFijos()
    {
        var rnd = new Random(42);
        var nombres = new[] {
            "ROCA BECK EDMUNDO PILAR", "ROCA ROCA MAX ANDRES", "ROCA JUSTINIANO JULIO CESAR",
            "MENDOZA GARCIA MARIA LUISA", "PEREZ RODRIGUEZ JUAN CARLOS", "GUTIERREZ FLORES ANA MARIA",
            "CASTRO MENDOZA LUIS ALBERTO", "VARGAS PEREZ CLAUDIA PATRICIA", "ROJAS GUTIERREZ PEDRO PABLO",
            "FERNANDEZ CASTRO ROSARIO", "MORALES VARGAS FERNANDO", "SILVA ROJAS PATRICIA ELENA",
            "HERRERA GUTIERREZ CARLOS ALBERTO", "LOPEZ FERNANDEZ MARCIA", "GARCIA MORALES ROBERTO",
            "RAMIREZ SILVA VERONICA", "TORRES HERRERA MIGUEL ANGEL", "DIAZ LOPEZ SANDRA KARINA",
            "ALVAREZ GARCIA EDGAR", "MEDINA RAMIREZ GLORIA ESTHER", "SOSA TORRES RICARDO",
            "VILLARROEL ALVAREZ CLARISA", "ARCE MEDINA JORGE LUIS", "QUISPE SOSA TERESA",
            "MAMANI VILLARROEL JOSE", "CONDORI ARCE EUGENIA", "APAZA QUISPE RODRIGO",
            "CHOQUE CONDORI LILIANA", "HUANCA APAZA MARCO ANTONIO", "TITO CHOQUE NELLY",
            "CRUZ HUANCA PABLO", "LLANOS CRUZ ROSA", "PEREIRA LLANOS JAVIER",
            "BUSTOS PEREIRA CLAUDIA", "AGUIRRE BUSTOS MAURICIO", "RIOS AGUIRRE CAROLINA",
            "NUÑEZ RIOS GUSTAVO", "CAMPOS NUÑEZ BEATRIZ", "DELGADO CAMPOS ALEJANDRO",
            "ESPINOZA DELGADO PAULA", "REYES ESPINOZA DANIEL", "MEDINA REYES LUCIA",
            "COPA MEDINA OSCAR", "SUNTA COPA IVONNE", "CHAVEZ SUNTA FREDDY",
            "QUISBERT CHAVEZ MONICA", "LAZO QUISBERT HUGO", "MENDOZA LAZO CECILIA",
            "IBARRA MENDOZA RAUL", "PANIAGUA IBARRA SUSANA", "APAZA PANIAGUA ERNESTO",
            "MAMANI APAZA YOLANDA", "TITO MAMANI ARIEL", "CHOQUE TITO GREGORIA",
            "HUANCA CHOQUE LIMBERT", "CRUZ HUANCA VICTORIA", "LLANOS CRUZ BENJAMIN",
            "SALAZAR LLANOS CLARA", "FLORES SALAZAR IVAN", "MENDEZ FLORES BEATRIZ",
            "RIVERA MENDEZ RODOLFO", "CASTILLO RIVERA OLGA", "GUTIERREZ CASTILLO TEODORO",
            "PEREZ GUTIERREZ ROSA", "RODRIGUEZ PEREZ NELIDA", "GARCIA RODRIGUEZ JOSE ANTONIO",
            "MENDOZA GARCIA NANCY", "VARGAS MENDOZA GONZALO", "PEREZ VARGAS LILIAN",
            "CASTRO PEREZ RAMIRO", "JUSTINIANO CASTRO GLADYS", "ROCA JUSTINIANO IVAN",
            "BECK ROCA MARCELO", "EDMUNDO BECK CECILIA", "PILAR EDMUNDO LUCIA",
            "TORREJON PILAR ALFREDO", "ARCE TORREJON GLORIA", "MEDINA ARCE FERNANDO",
            "SOSA MEDINA CLAUDIA", "TORRES SOSA MARCO", "HERRERA TORRES NIDIA",
            "GUTIERREZ HERRERA CARLOS", "FLORES GUTIERREZ PAULA", "ROJAS FLORES GUSTAVO",
            "SILVA ROJAS MARIELA", "MORALES SILVA RODRIGO", "VARGAS MORALES CLARO",
            "PEREZ VARGAS ELENA", "CASTRO PEREZ JOSE LUIS", "MENDOZA CASTRO ANDREA",
            "GARCIA MENDOZA OSCAR", "RAMIREZ GARCIA TATIANA", "LOPEZ RAMIREZ SERGIO",
            "DIAZ LOPEZ KARINA", "ALVAREZ DIAZ MAURICIO", "SOSA ALVAREZ PATRICIA",
            "TORRES SOSA LUCIA", "HERRERA TORRES ARMANDO", "GUTIERREZ HERRERA MIRTHA",
            "FLORES GUTIERREZ RUBEN", "ROJAS FLORES CARMEN", "SILVA ROJAS ENRIQUE"
        };

        var items = new List<object>();
        for (int i = 0; i < 100; i++)
        {
            byte estado = (byte)(rnd.Next(5) + 1);
            double lat = -17.760 + (rnd.NextDouble() * 0.03);
            double lon = -63.170 + (rnd.NextDouble() * 0.03);
            int uv = rnd.Next(1, 20);
            int mza = rnd.Next(1, 50);
            int lote = rnd.Next(1, 20);

            items.Add(new
            {
                IdCodigo = i + 1,
                CodF_SQL = 1000 + i,
                CodF_SIG = $"{uv:D2}-{mza:D2}-{lote:D2}",
                CodFijo = 400 + i,
                Nombre = nombres[i % nombres.Length],
                Estado = estado,
                FechaCambioEstado = DateTime.Now.AddDays(-rnd.Next(365)),
                IdLote = (int?)null,
                Longitud = (float?)lon,
                Latitud = (float?)lat,
                uv = $"{uv:D2}",
                mza = $"{mza:D2}",
                lote = $"{lote:D2}"
            });
        }
        return items;
    }

    private List<object> GetMockManzanas()
    {
        var items = new List<object>();
        var rnd = new Random(42);

        for (int u = 1; u <= 3; u++)
        {
            for (int m = 1; m <= 8; m++)
            {
                double baseLat = -17.770 + (u * 0.008);
                double baseLon = -63.180 + (m * 0.008);
                double size = 0.003;

                var coords = new[]
                {
                    new Coordinate(baseLon, baseLat),
                    new Coordinate(baseLon + size, baseLat),
                    new Coordinate(baseLon + size, baseLat + size),
                    new Coordinate(baseLon, baseLat + size),
                    new Coordinate(baseLon, baseLat)
                };

                var polygon = _geometryFactory.CreatePolygon(coords);
                var geoJson = new GeoJsonWriter().Write(polygon);

                items.Add(new
                {
                    IdManzana = (u - 1) * 8 + m,
                    UV = $"UV{u:D2}",
                    MZA = $"MZA{m:D2}",
                    geom = geoJson
                });
            }
        }
        return items;
    }

    private List<object> GetMockLotes()
    {
        var items = new List<object>();
        var rnd = new Random(42);

        for (int i = 0; i < 60; i++)
        {
            double baseLat = -17.770 + (rnd.NextDouble() * 0.02);
            double baseLon = -63.180 + (rnd.NextDouble() * 0.02);
            double size = 0.001;

            var coords = new[]
            {
                new Coordinate(baseLon, baseLat),
                new Coordinate(baseLon + size, baseLat),
                new Coordinate(baseLon + size, baseLat + size),
                new Coordinate(baseLon, baseLat + size),
                new Coordinate(baseLon, baseLat)
            };

            var polygon = _geometryFactory.CreatePolygon(coords);
            var geoJson = new GeoJsonWriter().Write(polygon);

            items.Add(new
            {
                IdLote = i + 1,
                NroLote = $"L{(i + 1):D3}",
                IdManzana = rnd.Next(1, 25),
                geom = geoJson
            });
        }
        return items;
    }

    private List<object> GetMockVias()
    {
        var items = new List<object>();

        var vias = new[]
        {
            ("Avenida Santa Cruz", "Avenida", new Coordinate[] {
                new(-63.190, -17.775), new(-63.160, -17.775) }),
            ("Avenida Grigota", "Avenida", new Coordinate[] {
                new(-63.185, -17.765), new(-63.155, -17.765) }),
            ("Calle 21 de Mayo", "Calle", new Coordinate[] {
                new(-63.175, -17.780), new(-63.175, -17.755) }),
            ("Calle Libertador", "Calle", new Coordinate[] {
                new(-63.180, -17.782), new(-63.180, -17.758) }),
            ("Calle Sucre", "Calle", new Coordinate[] {
                new(-63.170, -17.778), new(-63.170, -17.752) }),
            ("Calle Bolivar", "Calle", new Coordinate[] {
                new(-63.165, -17.780), new(-63.165, -17.755) }),
            ("Calle Junin", "Calle", new Coordinate[] {
                new(-63.188, -17.770), new(-63.158, -17.770) }),
            ("Calle Colón", "Calle", new Coordinate[] {
                new(-63.187, -17.768), new(-63.157, -17.768) }),
            ("Calle Pando", "Calle", new Coordinate[] {
                new(-63.186, -17.772), new(-63.156, -17.772) }),
            ("Calle Warnes", "Calle", new Coordinate[] {
                new(-63.183, -17.776), new(-63.163, -17.776) }),
        };

        foreach (var (nombre, tipo, coords) in vias)
        {
            var line = _geometryFactory.CreateLineString(coords);
            var geoJson = new GeoJsonWriter().Write(line);
            items.Add(new
            {
                IdVia = items.Count + 1,
                Nombre = nombre,
                TipoVia = tipo,
                geom = geoJson
            });
        }
        return items;
    }
}
