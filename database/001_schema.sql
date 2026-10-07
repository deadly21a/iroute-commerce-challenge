SET XACT_ABORT ON;
GO
IF OBJECT_ID('dbo.import_batch', 'U') IS NULL
CREATE TABLE dbo.import_batch (
    batch_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    file_name NVARCHAR(100) NOT NULL,
    file_hash BINARY(32) NOT NULL CONSTRAINT UQ_import_batch_hash UNIQUE,
    row_count INT NOT NULL,
    imported_at DATETIME2 NOT NULL CONSTRAINT DF_import_batch_date DEFAULT SYSUTCDATETIME()
);
GO
IF OBJECT_ID('dbo.commerce', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.commerce (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        batch_id UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.import_batch(batch_id),
        pc_processdate DATE NOT NULL,
        pc_nomcomred NVARCHAR(200) NOT NULL,
        pc_numdoc NVARCHAR(50) NOT NULL,
        pc_email NVARCHAR(254) NOT NULL,
        pc_telefono NVARCHAR(40) NOT NULL,
        pc_direccion NVARCHAR(300) NOT NULL
    );
    CREATE INDEX IX_commerce_processdate ON dbo.commerce(pc_processdate);
END;
GO
IF OBJECT_ID('dbo.commerce_quarantine', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.commerce_quarantine (
        id BIGINT NOT NULL PRIMARY KEY,
        batch_id UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.import_batch(batch_id),
        pc_processdate DATE NOT NULL,
        pc_nomcomred NVARCHAR(200) NOT NULL,
        pc_numdoc NVARCHAR(50) NOT NULL,
        pc_email NVARCHAR(254) NOT NULL,
        pc_telefono NVARCHAR(40) NOT NULL,
        pc_direccion NVARCHAR(300) NOT NULL,
        quarantined_at DATETIME2 NOT NULL CONSTRAINT DF_quarantine_date DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_quarantine_processdate_id ON dbo.commerce_quarantine(pc_processdate, id);
END;
GO
-- Se agrega explícitamente la columna motivo requerida por el enunciado.
IF COL_LENGTH('dbo.commerce_quarantine', 'motivo') IS NULL
ALTER TABLE dbo.commerce_quarantine ADD motivo NVARCHAR(500) NOT NULL CONSTRAINT DF_quarantine_motivo DEFAULT N'';
GO
IF TYPE_ID('dbo.CommerceImportType') IS NULL
EXEC(N'CREATE TYPE dbo.CommerceImportType AS TABLE (
    pc_processdate DATE NOT NULL,
    pc_nomcomred NVARCHAR(200) NOT NULL,
    pc_numdoc NVARCHAR(50) NOT NULL,
    pc_email NVARCHAR(254) NOT NULL,
    pc_telefono NVARCHAR(40) NOT NULL,
    pc_direccion NVARCHAR(300) NOT NULL
)');
GO
IF OBJECT_ID('dbo.app_user', 'U') IS NULL
CREATE TABLE dbo.app_user (
    id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    email NVARCHAR(254) NOT NULL CONSTRAINT UQ_app_user_email UNIQUE,
    display_name NVARCHAR(100) NOT NULL,
    password_hash NVARCHAR(500) NOT NULL
);
GO
