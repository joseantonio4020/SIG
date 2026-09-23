USE VisorDatosSIG;
GO

-- 1. Deduplicar Manzanas
WITH CTE_Mza AS (
    SELECT Geom, ROW_NUMBER() OVER (
        PARTITION BY CAST(Geom.STCentroid().STX AS DECIMAL(10,6)), CAST(Geom.STCentroid().STY AS DECIMAL(10,6)), COALESCE(UV_MZA, '')
        ORDER BY (SELECT NULL)
    ) as rn
    FROM dbo.Manzanas
    WHERE Geom IS NOT NULL
)
DELETE FROM CTE_Mza WHERE rn > 1;
GO

-- 2. Deduplicar Lotes
WITH CTE_Lotes AS (
    SELECT Geom, ROW_NUMBER() OVER (
        PARTITION BY CAST(Geom.STCentroid().STX AS DECIMAL(10,6)), CAST(Geom.STCentroid().STY AS DECIMAL(10,6)), COALESCE(NroLote, '')
        ORDER BY (SELECT NULL)
    ) as rn
    FROM dbo.Lotes
    WHERE Geom IS NOT NULL
)
DELETE FROM CTE_Lotes WHERE rn > 1;
GO

-- 3. Deduplicar Vías
WITH CTE_Vias AS (
    SELECT OBJECTID, ROW_NUMBER() OVER (
        PARTITION BY CAST(Geom.STStartPoint().STX AS DECIMAL(10,6)), CAST(Geom.STStartPoint().STY AS DECIMAL(10,6)), COALESCE(Nombre, '')
        ORDER BY (SELECT NULL)
    ) as rn
    FROM dbo.Vias
    WHERE Geom IS NOT NULL
)
DELETE FROM CTE_Vias WHERE rn > 1;
GO

SELECT 'Manzanas' as Tabla, COUNT(*) as Total FROM dbo.Manzanas
UNION ALL
SELECT 'Lotes', COUNT(*) FROM dbo.Lotes
UNION ALL
SELECT 'Vias', COUNT(*) FROM dbo.Vias
UNION ALL
SELECT 'CodigosFijos', COUNT(*) FROM dbo.CodigosFijos;
GO
