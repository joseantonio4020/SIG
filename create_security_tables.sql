USE VisorDatosSIG;
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Usuarios')
BEGIN
    CREATE TABLE dbo.Usuarios(
        IdUsuario INT IDENTITY PRIMARY KEY,
        Login NVARCHAR(50) NOT NULL UNIQUE,
        Nombre NVARCHAR(120) NOT NULL,
        PasswordHash VARBINARY(MAX) NULL,
        PasswordSalt VARBINARY(MAX) NULL,
        Iteraciones INT NOT NULL CONSTRAINT DF_Usuarios_Iteraciones DEFAULT(100000),
        Activo BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT(1),
        Rol NVARCHAR(50) NOT NULL CONSTRAINT DF_Usuarios_Rol DEFAULT('Consultor'),
        FechaCreacion DATETIME2 NOT NULL CONSTRAINT DF_Usuarios_Fecha DEFAULT(SYSDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'Rol')
BEGIN
    ALTER TABLE dbo.Usuarios ADD Rol NVARCHAR(50) NOT NULL CONSTRAINT DF_Usuarios_Rol DEFAULT('Consultor');
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BitacoraAccesos')
BEGIN
    CREATE TABLE dbo.BitacoraAccesos(
        IdAcceso INT IDENTITY PRIMARY KEY,
        Login NVARCHAR(50) NOT NULL,
        TipoEvento NVARCHAR(20) NOT NULL,
        FechaHora DATETIME2 NOT NULL CONSTRAINT DF_Bitacora_Fecha DEFAULT(SYSDATETIME()),
        DireccionIP NVARCHAR(50) NULL,
        Detalle NVARCHAR(500) NULL
    );
END
GO

-- Insertar usuario admin si no existe
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Login = 'admin')
BEGIN
    INSERT INTO dbo.Usuarios(Login, Nombre, PasswordHash, PasswordSalt, Iteraciones, Activo, Rol, FechaCreacion)
    VALUES (
        N'admin',
        N'Administrador del Sistema',
        0x599F3972C925D7D968B215FB5385865FDF961BCA18BB332E13AB9F014903F89B,
        0x82A4FC4EE67761C1A4E95BCD6BF9CF9B63344B9E665C839C8F9F654D1718B665,
        100000,
        1,
        'Administrador',
        SYSDATETIME()
    );
END
GO

-- Insertar usuario consultor si no existe (password: consultor123)
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Login = 'consultor')
BEGIN
    INSERT INTO dbo.Usuarios(Login, Nombre, PasswordHash, PasswordSalt, Iteraciones, Activo, Rol, FechaCreacion)
    VALUES (
        N'consultor',
        N'Usuario Consultor',
        0x599F3972C925D7D968B215FB5385865FDF961BCA18BB332E13AB9F014903F89B,
        0x82A4FC4EE67761C1A4E95BCD6BF9CF9B63344B9E665C839C8F9F654D1718B665,
        100000,
        1,
        'Consultor',
        SYSDATETIME()
    );
END
GO
