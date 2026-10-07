using System.Data;
using Commerce.Api.Models;
using Microsoft.Data.SqlClient;

namespace Commerce.Api.Data;

public sealed class CommerceRepository(IConfiguration configuration)
{
    public string ConnectionString => configuration.GetConnectionString("Commerce")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:Commerce.");

    public async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqlConnection(ConnectionString);
        try { await connection.OpenAsync(ct); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }

    public async Task<ImportResult> ImportAsync(string fileName, IReadOnlyList<CommerceRow> rows, byte[] hash, CancellationToken ct)
    {
        using var table = new DataTable();
        table.Columns.Add("pc_processdate", typeof(DateTime));
        foreach (var name in Services.CsvImportService.Headers.Skip(1)) table.Columns.Add(name, typeof(string));
        foreach (var row in rows)
            table.Rows.Add(row.PcProcessdate.ToDateTime(TimeOnly.MinValue), row.PcNomcomred, row.PcNumdoc,
                row.PcEmail, row.PcTelefono, row.PcDireccion);
        await using var connection = await OpenAsync(ct);
        await using var command = new SqlCommand("dbo.sp_create_commerce", connection) { CommandType = CommandType.StoredProcedure };
        var batch = Guid.NewGuid();
        command.Parameters.Add("@batch_id", SqlDbType.UniqueIdentifier).Value = batch;
        command.Parameters.Add("@file_name", SqlDbType.NVarChar, 100).Value = fileName;
        command.Parameters.Add("@file_hash", SqlDbType.Binary, 32).Value = hash;
        command.Parameters.Add(new SqlParameter("@rows", SqlDbType.Structured) { TypeName = "dbo.CommerceImportType", Value = table });
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new(batch, fileName, reader.GetInt32(0), reader.GetDateTime(1));
    }

    public async Task<ProcessResult> ProcessAsync(DateOnly date, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = new SqlCommand("dbo.sp_process_commerce", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@process_date", SqlDbType.Date).Value = date.ToDateTime(TimeOnly.MinValue);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new(date, reader.GetInt32(0), reader.GetInt32(1));
    }

    public async Task<PageResult<QuarantineRow>> GetQuarantineAsync(DateOnly? date, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = new SqlCommand("dbo.sp_list_commerce_quarantine", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@process_date", SqlDbType.Date).Value = date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        command.Parameters.Add("@offset", SqlDbType.Int).Value = (page - 1) * pageSize;
        command.Parameters.Add("@page_size", SqlDbType.Int).Value = pageSize;
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        var total = reader.GetInt32(0);
        await reader.NextResultAsync(ct);
        var items = new List<QuarantineRow>();
        while (await reader.ReadAsync(ct))
            items.Add(new(reader.GetInt64(0), reader.GetGuid(1), DateOnly.FromDateTime(reader.GetDateTime(2)),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
                reader.GetString(8), reader.GetDateTime(9)));
        return new(items, total, page, pageSize);
    }

    public async Task<Overview> GetOverviewAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = new SqlCommand("dbo.sp_commerce_overview", connection) { CommandType = CommandType.StoredProcedure };
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        var commerce = reader.GetInt32(0); var quarantine = reader.GetInt32(1); var imports = reader.GetInt32(2);
        await reader.NextResultAsync(ct);
        var dates = new List<DateSummary>();
        while (await reader.ReadAsync(ct)) dates.Add(new(DateOnly.FromDateTime(reader.GetDateTime(0)), reader.GetInt32(1), reader.GetInt32(2)));
        await reader.NextResultAsync(ct);
        var batches = new List<BatchSummary>();
        while (await reader.ReadAsync(ct)) batches.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2), reader.GetDateTime(3)));
        return new(commerce, quarantine, imports, dates, batches);
    }

    public async Task<AppUser?> FindUserAsync(string email, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = new SqlCommand("SELECT id,email,display_name,password_hash FROM dbo.app_user WHERE email=@email", connection);
        command.Parameters.Add("@email", SqlDbType.NVarChar, 254).Value = email.Trim().ToLowerInvariant();
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)) : null;
    }
}
