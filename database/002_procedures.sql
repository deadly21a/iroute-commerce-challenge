CREATE OR ALTER PROCEDURE dbo.sp_create_commerce
    @batch_id UNIQUEIDENTIFIER,
    @file_name NVARCHAR(100),
    @file_hash BINARY(32),
    @rows dbo.CommerceImportType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @count INT = (SELECT COUNT(*) FROM @rows);
    IF @count = 0 THROW 50001, 'El archivo no contiene registros.', 1;
    BEGIN TRY
        BEGIN TRANSACTION;
        INSERT dbo.import_batch(batch_id, file_name, file_hash, row_count)
        VALUES (@batch_id, @file_name, @file_hash, @count);
        INSERT dbo.commerce(batch_id, pc_processdate, pc_nomcomred, pc_numdoc, pc_email, pc_telefono, pc_direccion)
        SELECT @batch_id, pc_processdate, pc_nomcomred, pc_numdoc, pc_email, pc_telefono, pc_direccion FROM @rows;
        COMMIT;
        SELECT @count AS inserted_count, imported_at FROM dbo.import_batch WHERE batch_id=@batch_id;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_process_commerce @process_date DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @moved TABLE (
            id BIGINT, batch_id UNIQUEIDENTIFIER, pc_processdate DATE,
            pc_nomcomred NVARCHAR(200), pc_numdoc NVARCHAR(50), pc_email NVARCHAR(254),
            pc_telefono NVARCHAR(40), pc_direccion NVARCHAR(300), motivo NVARCHAR(500)
        );
        -- DELETE OUTPUT conserva los datos en una tabla temporal dentro de la transacción.
        -- OUTPUT INTO no admite un destino con claves foráneas; el INSERT posterior las verifica.
        -- HOLDLOCK serializa operaciones sobre la misma fecha durante la transacción.
        DELETE c
        OUTPUT deleted.id, deleted.batch_id, deleted.pc_processdate,
            deleted.pc_nomcomred, deleted.pc_numdoc, deleted.pc_email,
            deleted.pc_telefono, deleted.pc_direccion,
            CONCAT_WS(N'; ',
                CASE WHEN LEN(TRIM(N' ' + NCHAR(9) + NCHAR(10) + NCHAR(13) + NCHAR(160) FROM deleted.pc_nomcomred))=0
                     THEN N'El nombre del comercio (nomcomred) se encuentra vacío' END,
                CASE WHEN LEN(TRIM(N' ' + NCHAR(9) + NCHAR(10) + NCHAR(13) + NCHAR(160) FROM deleted.pc_numdoc))=0
                     THEN N'El número de documento (numdoc) se encuentra vacío'
                     WHEN deleted.pc_numdoc COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%'
                     THEN N'El número de documento (numdoc) contiene letras o caracteres especiales' END)
        INTO @moved (id,batch_id,pc_processdate,pc_nomcomred,pc_numdoc,pc_email,pc_telefono,pc_direccion,motivo)
        FROM dbo.commerce c WITH (UPDLOCK,HOLDLOCK)
        WHERE c.pc_processdate=@process_date AND (
            LEN(TRIM(N' ' + NCHAR(9) + NCHAR(10) + NCHAR(13) + NCHAR(160) FROM c.pc_nomcomred))=0 OR
            LEN(TRIM(N' ' + NCHAR(9) + NCHAR(10) + NCHAR(13) + NCHAR(160) FROM c.pc_numdoc))=0 OR
            c.pc_numdoc COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%');
        DECLARE @quarantined INT = @@ROWCOUNT;
        INSERT dbo.commerce_quarantine(id,batch_id,pc_processdate,pc_nomcomred,pc_numdoc,pc_email,pc_telefono,pc_direccion,motivo)
        SELECT id,batch_id,pc_processdate,pc_nomcomred,pc_numdoc,pc_email,pc_telefono,pc_direccion,motivo FROM @moved;
        DECLARE @remaining INT = (SELECT COUNT(*) FROM dbo.commerce WHERE pc_processdate=@process_date);
        COMMIT;
        SELECT @quarantined AS quarantined_count, @remaining AS remaining_count;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_list_commerce_quarantine
    @process_date DATE = NULL, @offset INT = 0, @page_size INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    -- Dos consultas en la misma instantánea lógica para mantener total y página consistentes.
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRY
        BEGIN TRANSACTION;
        SELECT COUNT(*) AS total_count FROM dbo.commerce_quarantine
        WHERE @process_date IS NULL OR pc_processdate=@process_date;
        SELECT id,batch_id,pc_processdate,pc_nomcomred,pc_numdoc,pc_email,pc_telefono,pc_direccion,motivo,quarantined_at
        FROM dbo.commerce_quarantine
        WHERE @process_date IS NULL OR pc_processdate=@process_date
        ORDER BY id DESC OFFSET @offset ROWS FETCH NEXT @page_size ROWS ONLY;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_commerce_overview
AS
BEGIN
    SET NOCOUNT ON;
    SELECT (SELECT COUNT(*) FROM dbo.commerce) AS commerce_count,
           (SELECT COUNT(*) FROM dbo.commerce_quarantine) AS quarantine_count,
           (SELECT COUNT(*) FROM dbo.import_batch) AS import_count;
    SELECT pc_processdate,
           SUM(pending_count) AS pending_count, SUM(quarantined_count) AS quarantined_count
    FROM (
        SELECT pc_processdate, COUNT(*) AS pending_count, 0 AS quarantined_count FROM dbo.commerce GROUP BY pc_processdate
        UNION ALL
        SELECT pc_processdate, 0, COUNT(*) FROM dbo.commerce_quarantine GROUP BY pc_processdate
    ) dates GROUP BY pc_processdate ORDER BY pc_processdate DESC;
    SELECT TOP(5) batch_id, file_name, row_count, imported_at FROM dbo.import_batch ORDER BY imported_at DESC;
END;
GO
