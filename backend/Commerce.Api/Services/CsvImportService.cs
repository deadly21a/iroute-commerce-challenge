using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Commerce.Api.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace Commerce.Api.Services;

public sealed class ImportValidationException(string message) : Exception(message);
public sealed record ParsedImport(IReadOnlyList<CommerceRow> Rows, byte[] Hash);

public sealed partial class CsvImportService
{
    public const int MaxBytes = 10 * 1024 * 1024;
    public const int MaxRows = 50_000;
    public static readonly string[] Headers =
        ["pc_processdate", "pc_nomcomred", "pc_numdoc", "pc_email", "pc_telefono", "pc_direccion"];

    [GeneratedRegex(@"^commerce_(\d{8})\.csv$", RegexOptions.CultureInvariant)]
    private static partial Regex FileNamePattern();

    public async Task<ParsedImport> ParseAsync(Stream stream, string fileName, CancellationToken ct)
    {
        var match = FileNamePattern().Match(fileName);
        if (!match.Success || !DateOnly.TryParseExact(match.Groups[1].Value, "ddMMyyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            throw new ImportValidationException("El archivo debe llamarse commerce_DDMMYYYY.csv con una fecha válida.");

        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            if (memory.Length + read > MaxBytes)
                throw new ImportValidationException("El archivo supera el límite de 10 MB.");
            await memory.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        if (memory.Length == 0) throw new ImportValidationException("El archivo está vacío.");
        var hash = SHA256.HashData(memory.GetBuffer().AsSpan(0, (int)memory.Length));
        memory.Position = 0;
        try
        {
            using var reader = new StreamReader(memory, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
                DetectColumnCountChanges = true,
                ExceptionMessagesContainRawData = false
            });
            if (!await csv.ReadAsync()) throw new ImportValidationException("El archivo no contiene registros.");
            csv.ReadHeader();
            var header = csv.HeaderRecord ?? [];
            if (header.Length > 0) header[0] = header[0].TrimStart('\uFEFF');
            if (!header.SequenceEqual(Headers))
                throw new ImportValidationException($"Encabezados esperados, en este orden: {string.Join(",", Headers)}.");
            var rows = new List<CommerceRow>();
            while (await csv.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();
                if (rows.Count >= MaxRows) throw new ImportValidationException("Máximo 50 000 registros por archivo.");
                var dateText = csv.GetField(0) ?? "";
                if (!DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    throw new ImportValidationException($"Fila {csv.Parser.Row}: pc_processdate debe tener formato yyyy-MM-dd.");
                var values = Enumerable.Range(1, 5).Select(i => csv.GetField(i) ?? "").ToArray();
                int[] limits = [200, 50, 254, 40, 300];
                for (int i = 0; i < values.Length; i++)
                    if (values[i].Length > limits[i])
                        throw new ImportValidationException($"Fila {csv.Parser.Row}: {Headers[i + 1]} supera {limits[i]} caracteres.");
                rows.Add(new(date, values[0], values[1], values[2], values[3], values[4]));
            }
            if (rows.Count == 0) throw new ImportValidationException("El archivo debe contener al menos un registro además del encabezado.");
            return new(rows, hash);
        }
        catch (DecoderFallbackException)
        {
            throw new ImportValidationException("El archivo debe estar codificado en UTF-8.");
        }
        catch (CsvHelperException)
        {
            throw new ImportValidationException("CSV inválido: revise comillas, separadores y cantidad de columnas.");
        }
    }
}
