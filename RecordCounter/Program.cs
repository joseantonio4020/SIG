using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

var factory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
string basePath = @"C:\Users\ja994\OneDrive\Documentos\bolivia\sistemas de informacion geografica\proyecto\Especificaciones_Proyecto_VisorDatosSIG_2026\DatosSIG_Reproj";

string[] layers = {
    "Exp_CodigoFijo_4326",
    "Exp_MapaBase_LOTES_4326", 
    "Exp_MapaBase_MZA_4326",
    "Exp_MapaBase_VIAS_4326"
};

Console.WriteLine("=== CONTEO DE REGISTROS EN SHAPEFILES ===");
Console.WriteLine();

foreach (var layer in layers)
{
    string shpFile = System.IO.Path.Combine(basePath, $"{layer}.shp");
    if (!System.IO.File.Exists(shpFile))
    {
        Console.WriteLine($"{layer}: ARCHIVO NO ENCONTRADO");
        continue;
    }
    
    int count = 0;
    try
    {
        using var reader = new ShapefileDataReader(shpFile, factory);
        while (reader.Read()) count++;
    }
    catch (Exception ex) { count = -1; Console.WriteLine($"{layer}: ERROR - {ex.Message}"); }
    
    Console.WriteLine($"{layer,-20}: {count,6} registros");
}

Console.WriteLine();
Console.WriteLine("=========================================");