USE VisorDatosSIG;
GO

-- 1. Eliminar filas duplicadas conservando la más reciente de cada CodF_SQL
WITH CTE AS (
    SELECT CodF_SQL, ROW_NUMBER() OVER (PARTITION BY CodF_SQL ORDER BY FechaCambioEstado DESC) as rn
    FROM dbo.CodigosFijos
)
DELETE FROM CTE WHERE rn > 1;
GO

-- 2. Asegurar que CodF_SQL es NOT NULL y crear Clave Primaria para evitar duplicados
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodigosFijos')
BEGIN
    ALTER TABLE dbo.CodigosFijos ALTER COLUMN CodF_SQL INT NOT NULL;
    ALTER TABLE dbo.CodigosFijos ADD CONSTRAINT PK_CodigosFijos PRIMARY KEY (CodF_SQL);
END
GO

-- 3. Asignar distribución de estados consistente y sin duplicidad
-- Primero poner todos en Normal (1)
UPDATE dbo.CodigosFijos SET Estado = 1, FechaCambioEstado = SYSDATETIME();

-- Asignar Para corte (2) a aprox 20% de los registros
WITH ParaCorte AS (
    SELECT TOP (1215) Estado, FechaCambioEstado 
    FROM dbo.CodigosFijos 
    WHERE Estado = 1
    ORDER BY CHECKSUM(HASHBYTES('MD5', CAST(CodF_SQL AS VARCHAR)))
)
UPDATE ParaCorte SET Estado = 2, FechaCambioEstado = SYSDATETIME();

-- Asignar Cortado (3) a aprox 12% de los registros
WITH Cortados AS (
    SELECT TOP (730) Estado, FechaCambioEstado 
    FROM dbo.CodigosFijos 
    WHERE Estado = 1
    ORDER BY CHECKSUM(HASHBYTES('MD5', CAST(CodF_SQL AS VARCHAR)))
)
UPDATE Cortados SET Estado = 3, FechaCambioEstado = SYSDATETIME();

-- Asignar Baja parcial (4) a aprox 6% de los registros
WITH BajaParcial AS (
    SELECT TOP (365) Estado, FechaCambioEstado 
    FROM dbo.CodigosFijos 
    WHERE Estado = 1
    ORDER BY CHECKSUM(HASHBYTES('MD5', CAST(CodF_SQL AS VARCHAR)))
)
UPDATE BajaParcial SET Estado = 4, FechaCambioEstado = SYSDATETIME();

-- Asignar Baja total (5) a aprox 2% de los registros
WITH BajaTotal AS (
    SELECT TOP (120) Estado, FechaCambioEstado 
    FROM dbo.CodigosFijos 
    WHERE Estado = 1
    ORDER BY CHECKSUM(HASHBYTES('MD5', CAST(CodF_SQL AS VARCHAR)))
)
UPDATE BajaTotal SET Estado = 5, FechaCambioEstado = SYSDATETIME();
GO

-- 4. Verificar conteo por estado
SELECT Estado, COUNT(*) as Cantidad 
FROM dbo.CodigosFijos 
GROUP BY Estado 
ORDER BY Estado;
GO
