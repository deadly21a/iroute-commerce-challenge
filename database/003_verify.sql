-- Consultas de solo lectura. Ejecutar en IRouteCommerce después de la demostración.
SELECT COUNT(*) AS registros_en_commerce FROM dbo.commerce;
SELECT COUNT(*) AS registros_en_cuarentena FROM dbo.commerce_quarantine;

SELECT pc_processdate,COUNT(*) AS conservados
FROM dbo.commerce GROUP BY pc_processdate ORDER BY pc_processdate;

SELECT pc_processdate,COUNT(*) AS cuarentena
FROM dbo.commerce_quarantine GROUP BY pc_processdate ORDER BY pc_processdate;

SELECT id,pc_processdate,pc_nomcomred,pc_numdoc FROM dbo.commerce ORDER BY id;
SELECT id,pc_processdate,pc_nomcomred,pc_numdoc,motivo FROM dbo.commerce_quarantine ORDER BY id;

-- Ningún registro debe existir simultáneamente en ambas tablas.
SELECT c.id FROM dbo.commerce c INNER JOIN dbo.commerce_quarantine q ON c.id=q.id;

-- Cada lote debe conservar exactamente la cantidad de filas importadas.
SELECT b.batch_id,b.file_name,b.row_count,
    (SELECT COUNT(*) FROM dbo.commerce c WHERE c.batch_id=b.batch_id) AS commerce_count,
    (SELECT COUNT(*) FROM dbo.commerce_quarantine q WHERE q.batch_id=b.batch_id) AS quarantine_count
FROM dbo.import_batch b ORDER BY b.imported_at;
