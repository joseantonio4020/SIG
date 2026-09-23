-- Script de configuración inicial de la base de datos VisorDatosSIG
-- Ejecutar en SQL Server Management Studio 2022

-- Crear base de datos si no existe
IF DB_ID(N'VisorDatosSIG') IS NULL 
    CREATE DATABASE VisorDatosSIG;
GO

USE VisorDatosSIG;
GO

-- ============================================
-- TABLAS DE SEGURIDAD Y ROLES
-- ============================================

CREATE TABLE dbo.Usuarios(
    IdUsuario INT IDENTITY PRIMARY KEY,
    Login NVARCHAR(50) NOT NULL UNIQUE,
    Nombre NVARCHAR(120) NOT NULL,
    PasswordHash VARBINARY(32) NOT NULL,
    PasswordSalt VARBINARY(32) NOT NULL,
    Iteraciones INT NOT NULL CONSTRAINT DF_Usuarios_Iteraciones DEFAULT(100000),
    Activo BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT(1),
    FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_Usuarios_Fecha DEFAULT(SYSDATETIME())
);
GO

CREATE TABLE dbo.Roles(
    IdRol INT IDENTITY(1,1) PRIMARY KEY,
    NombreRol VARCHAR(50) NOT NULL UNIQUE,
    Descripcion VARCHAR(200) NULL,
    Estado BIT NOT NULL CONSTRAINT DF_Roles_Estado DEFAULT(1)
);
GO

CREATE TABLE dbo.UsuariosRoles(
    IdUsuarioRol INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL,
    IdRol INT NOT NULL,
    CONSTRAINT FK_UsuariosRoles_Usuarios FOREIGN KEY(IdUsuario) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT FK_UsuariosRoles_Roles FOREIGN KEY(IdRol) REFERENCES dbo.Roles(IdRol),
    CONSTRAINT UQ_UsuariosRoles UNIQUE(IdUsuario,IdRol)
);
GO

-- Insertar roles iniciales
INSERT dbo.Roles(NombreRol,Descripcion) VALUES
('Administrador','Administrador del Sistema'),
('Catastro','Rol para crear, modificar y eliminar Manzanas, Lotes, Codigo Fijo y Vias'),
('Lecturador','Rol para lecturar medidores'),
('Cortador','Rol para cortar servicios'),
('Reconexion','Rol para la reconexion por corte');
GO

-- Usuario admin inicial (password: Admin123! con 100000 iteraciones PBKDF2)
INSERT dbo.Usuarios(Login,Nombre,PasswordHash,PasswordSalt,Iteraciones,Activo)
VALUES(
    N'admin',
    N'Administrador',
    0x599F3972C925D7D968B215FB5385865FDF961BCA18BB332E13AB9F014903F89B,
    0x82A4FC4EE67761C1A4E95BCD6BF9CF9B63344B9E665C839C8F9F654D1718B665,
    100000,
    1
);
GO

-- Asignar admin a todos los roles
INSERT dbo.UsuariosRoles(IdUsuario,IdRol)
SELECT u.IdUsuario, r.IdRol
FROM dbo.Usuarios u CROSS JOIN dbo.Roles r
WHERE u.Login='admin';

-- ============================================
-- TABLAS CATASTRALAS
-- ============================================

CREATE TABLE dbo.MenuOpciones(
    IdMenu INT IDENTITY(1,1) PRIMARY KEY,
    IdMenuPadre INT NULL,
    Nivel INT NOT NULL,
    NombreMenu NVARCHAR(100) NOT NULL,
    Url NVARCHAR(200) NULL,
    Icono NVARCHAR(50) NULL,
    Orden INT NOT NULL CONSTRAINT DF_MenuOpciones_Orden DEFAULT(0),
    Estado BIT NOT NULL CONSTRAINT DF_MenuOpciones_Estado DEFAULT(1),
    CONSTRAINT FK_MenuOpciones_Padre FOREIGN KEY(IdMenuPadre) REFERENCES dbo.MenuOpciones(IdMenu),
    CONSTRAINT CK_MenuOpciones_Nivel CHECK(Nivel IN(1,2,3))
);
GO

CREATE TABLE dbo.UsuarioMenu(
    IdUsuarioMenu INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL,
    IdMenu INT NOT NULL,
    PuedeVer BIT NOT NULL CONSTRAINT DF_UsuarioMenu_PuedeVer DEFAULT(1),
    PuedeCrear BIT NOT NULL CONSTRAINT DF_UsuarioMenu_PuedeCrear DEFAULT(0),
    PuedeEditar BIT NOT NULL CONSTRAINT DF_UsuarioMenu_PuedeEditar DEFAULT(0),
    PuedeEliminar BIT NOT NULL CONSTRAINT DF_UsuarioMenu_PuedeEliminar DEFAULT(0),
    CONSTRAINT FK_UsuarioMenu_Usuarios FOREIGN KEY(IdUsuario) REFERENCES dbo.Usuarios(IdUsuario),
    CONSTRAINT FK_UsuarioMenu_Menu FOREIGN KEY(IdMenu) REFERENCES dbo.MenuOpciones(IdMenu),
    CONSTRAINT UQ_UsuarioMenu UNIQUE(IdUsuario,IdMenu)
);
GO

-- ============================================
-- TABLAS DE DATOS CATASTRALS
-- ============================================

CREATE TABLE dbo.CodigosFijos(
    IdCodigo INT IDENTITY PRIMARY KEY,
    CodF_SQL INT NULL,
    CodF_SIG NVARCHAR(25) NULL,
    CodFijo INT NULL,
    Nombre NVARCHAR(120) NULL,
    Estado TINYINT NOT NULL CONSTRAINT DF_CodigosFijos_Estado DEFAULT(1) WITH VALUES,
    FechaCambioEstado DATETIME2 NOT NULL CONSTRAINT DF_CodigosFijos_FechaCambioEstado DEFAULT(SYSDATETIME()) WITH VALUES,
    IdLote INT NULL,
    Longitud FLOAT NULL,
    Latitud FLOAT NULL,
    Geom geometry NULL,
    CONSTRAINT CK_CodigosFijos_Estado CHECK (Estado BETWEEN 1 AND 5)
);
GO

CREATE INDEX IX_CodigosFijos_CodFijo ON dbo.CodigosFijos(CodFijo);
CREATE INDEX IX_CodigosFijos_Nombre ON dbo.CodigosFijos(Nombre);
CREATE INDEX IX_CodigosFijos_Estado ON dbo.CodigosFijos(Estado);
CREATE INDEX IX_CodigosFijos_IdLote ON dbo.CodigosFijos(IdLote);

-- Índice espacial para queries espaciales
CREATE SPATIAL INDEX SIX_CodigosFijos_Geom ON dbo.CodigosFijos(Geom) USING GEOMETRY_GRID WITH (BOUNDING_BOX=(-180,-90,180,90));
GO

CREATE TABLE dbo.Manzanas(
    IdManzana INT IDENTITY PRIMARY KEY,
    IdOrigen INT NULL,
    UV_MZA NVARCHAR(20) NULL,
    UV NVARCHAR(15) NULL,
    MZA NVARCHAR(10) NULL,
    Geom geometry NULL
);
GO

CREATE INDEX IX_Manzanas_UV_MZA ON dbo.Manzanas(UV,MZA);
GO

-- Índice espacial
CREATE SPATIAL INDEX SIX_Manzanas_Geom ON dbo.Manzanas(Geom) USING GEOMETRY_GRID WITH (BOUNDING_BOX=(-180,-90,180,90));
GO

CREATE TABLE dbo.Lotes(
    IdLote INT IDENTITY PRIMARY KEY,
    IdOrigen INT NULL,
    NroLote NVARCHAR(15) NULL,
    IdManzana INT NULL,
    Geom geometry NULL,
    CONSTRAINT FK_Lotes_Manzanas FOREIGN KEY(IdManzana) REFERENCES dbo.Manzanas(IdManzana)
);
GO

CREATE INDEX IX_Lotes_NroLote ON dbo.Lotes(NroLote);
GO

-- FK: Lote -> CodigosFijos
ALTER TABLE dbo.CodigosFijos WITH CHECK ADD CONSTRAINT FK_CodigosFijos_Lotes
    FOREIGN KEY(IdLote) REFERENCES dbo.Lotes(IdLote);
GO

CREATE TABLE dbo.Vias(
    IdVia INT IDENTITY PRIMARY KEY,
    OBJECTID INT NULL,
    Nombre NVARCHAR(40) NULL,
    TipoVia NVARCHAR(30) NULL,
    OSMID NVARCHAR(20) NULL,
    Geom geometry NULL
);
GO

-- ============================================
-- TRIGGERS Y PROCEDURES
-- ============================================

-- Trigger: ActualizarFechaCambioEstado en CodigosFigos
CREATE TRIGGER dbo.TR_CodigosFijos_FechaCambioEstado
ON dbo.CodigosFijos
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(Estado) RETURN;
    UPDATE c SET FechaCambioEstado = SYSDATETIME()
    FROM dbo.CodigosFijos c
    JOIN inserted i ON i.IdCodigo = c.IdCodigo
    JOIN deleted d ON d.IdCodigo = i.IdCodigo
    WHERE i.Estado <> d.Estado;
END;
GO

-- Procedimiento: BuscarInmueble (adaptado para vistas)
CREATE OR ALTER PROCEDURE dbo.sp_BuscarInmueble
    @Texto NVARCHAR(100)=NULL,
    @UV NVARCHAR(15)=NULL,
    @Mza NVARCHAR(10)=NULL,
    @Lote NVARCHAR(15)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP(100) c.IdCodigo, c.CodFijo, c.Nombre, c.Estado, c.FechaCambioEstado,
        m.UV, m.MZA, l.NroLote,
        COALESCE(c.Latitud, c.Geom.STY) AS Latitud,
        COALESCE(c.Longitud, c.Geom.STX) AS Longitud
    FROM dbo.CodigosFijos c
    LEFT JOIN dbo.Lotes l ON l.IdLote=c.IdLote
    LEFT JOIN dbo.Manzanas m ON m.IdManzana=l.IdManzana
    WHERE (@Texto IS NULL OR CONVERT(NVARCHAR(30),c.CodFijo)=@Texto OR c.Nombre LIKE N'%' + @Texto + N'%')
      AND (@UV IS NULL OR m.UV=@UV)
      AND (@Mza IS NULL OR m.MZA=@Mza)
      AND (@Lote IS NULL OR l.NroLote=@Lote)
    ORDER BY c.CodFijo;
END;
GO

-- Procedimiento: ActualizarLoteCodigosFijos
CREATE OR ALTER PROCEDURE dbo.sp_ActualizarLoteCodigosFijos
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Completar IdManzana en lotes sin vincular
    UPDATE l SET IdManzana = a.IdManzana
    FROM dbo.Lotes l
    CROSS APPLY (
        SELECT TOP (1) m.IdManzana FROM dbo.Manzanas m
        WHERE l.Geom IS NOT NULL AND m.Geom IS NOT NULL
          AND m.Geom.STSrid = l.Geom.STSrid
          AND m.Geom.Filter(l.Geom.STPointOnSurface()) = 1
          AND m.Geom.STIntersects(l.Geom.STPointOnSurface()) = 1
        ORDER BY m.IdManzana
    ) a WHERE l.IdManzana IS NULL;

    -- Actualizar IdLote en CodigosFijos
    UPDATE c SET IdLote = a.IdLote
    FROM dbo.CodigosFijos c
    OUTER APPLY (
        SELECT TOP (1) l.IdLote FROM dbo.Lotes l
        WHERE c.Geom IS NOT NULL AND l.Geom IS NOT NULL
          AND l.Geom.STSrid = c.Geom.STSrid
          AND l.Geom.Filter(c.Geom) = 1
          AND l.Geom.STIntersects(c.Geom) = 1
        ORDER BY l.Geom.STArea(), l.IdLote
    ) a WHERE ISNULL(c.IdLote, -1) <> ISNULL(a.IdLote, -1);

    -- Reportes
    SELECT 'TotalCodigos' = COUNT(*), 'AsociadosALote' = COUNT(IdLote), 'SinLote' = COUNT(*) - COUNT(IdLote) FROM dbo.CodigosFijos;
    SELECT 'TotalLotes' = COUNT(*), 'AsociadosAManzana' = COUNT(IdManzana), 'SinManzana' = COUNT(*) - COUNT(IdManzana) FROM dbo.Lotes;
END;
GO

-- Ejecutar procedimiento de actualización
EXEC dbo.sp_ActualizarLoteCodigosFijos;
GO

-- ============================================
-- TABLA DE BITACORA DE MIGRACION
-- ============================================

CREATE TABLE dbo.BitacoraMigracion(
    IdBitacora INT IDENTITY PRIMARY KEY,
    FechaMigracion DATETIME2 NOT NULL DEFAULT(SYSDATETIME()),
    Usuario NVARCHAR(100) NOT NULL,
    CapasMigradas INT NOT NULL DEFAULT(0),
    TotalRegistros INT NOT NULL DEFAULT(0),
    Estado NVARCHAR(20) NOT NULL DEFAULT('Pendiente'),
    Observaciones NVARCHAR(500) NULL
);
GO

PRINT '=== Base de datos VisorDatosSIG configurada exitosamente ===';
GO