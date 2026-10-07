using System.Security.Cryptography;
using Commerce.Api.Data;
using Commerce.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Commerce.Tests;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMMERCE_TEST_CONNECTION")))
            Skip = "Configura COMMERCE_TEST_CONNECTION para ejecutar contra SQL Server real.";
    }
}

public sealed class SqlIntegrationTests
{
    private static CommerceRow Row(DateOnly date, string name, string document) => new(date, name, document, "test@example.com", "", "Dirección ficticia");

    private static async Task WithDatabase(Func<CommerceRepository, Task> test)
    {
        var source = Environment.GetEnvironmentVariable("COMMERCE_TEST_CONNECTION")!;
        var builder = new SqlConnectionStringBuilder(source) { InitialCatalog = "IRouteCommerceTests_" + Guid.NewGuid().ToString("N") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Commerce"] = builder.ConnectionString }).Build();
        var repo = new CommerceRepository(config);
        var env = new TestEnvironment();
        try
        {
            await DatabaseInitializer.InitializeAsync(repo, config, env);
            await test(repo);
        }
        finally
        {
            // Solo se elimina la base temporal cuyo nombre generó esta prueba.
            var database = builder.InitialCatalog;
            builder.InitialCatalog = "master";
            SqlConnection.ClearAllPools();
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand($"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END", connection);
            command.Parameters.AddWithValue("@name", database);
            await command.ExecuteNonQueryAsync();
        }
    }

    [SqlFact]
    public Task MovesAllInvalidRowsAndStoresAllReasons() => WithDatabase(async repo =>
    {
        var date = new DateOnly(2026, 10, 7);
        CommerceRow[] rows = [Row(date, "Válido", "00123"), Row(date, "", "123"), Row(date, "  \t", ""),
            Row(date, "Letras", "12A"), Row(date, "Símbolos", "12-3"), Row(date, "Unicode", "٠١٢"), Row(date, "Espacios", " 123 ")];
        await repo.ImportAsync("commerce_07102026.csv", rows, RandomNumberGenerator.GetBytes(32), default);
        var processed = await repo.ProcessAsync(date, default);
        Assert.Equal(6, processed.QuarantinedCount);
        Assert.Equal(1, processed.RemainingCount);
        var quarantine = await repo.GetQuarantineAsync(date, 1, 100, default);
        Assert.Equal(6, quarantine.TotalCount);
        var both = Assert.Single(quarantine.Items, row => row.PcNomcomred == "  \t");
        Assert.Contains("nombre", both.Motivo);
        Assert.Contains("documento", both.Motivo);
        Assert.All(quarantine.Items, row => Assert.False(string.IsNullOrWhiteSpace(row.Motivo)));
    });

    [SqlFact]
    public Task DateFilteringAndRepeatedProcessingAreSafe() => WithDatabase(async repo =>
    {
        var date = new DateOnly(2026, 10, 7); var next = date.AddDays(1);
        await repo.ImportAsync("commerce_07102026.csv", [Row(date, "", "1"), Row(next, "", "2")], RandomNumberGenerator.GetBytes(32), default);
        Assert.Equal(1, (await repo.ProcessAsync(date, default)).QuarantinedCount);
        Assert.Equal(0, (await repo.ProcessAsync(date, default)).QuarantinedCount);
        Assert.Equal(1, (await repo.GetOverviewAsync(default)).CommerceCount);
        Assert.Empty((await repo.GetQuarantineAsync(next, 1, 10, default)).Items);
    });

    [SqlFact]
    public Task DuplicateImportRollsBackWithoutDuplicatingRows() => WithDatabase(async repo =>
    {
        var hash = RandomNumberGenerator.GetBytes(32);
        CommerceRow[] rows = [Row(new(2026, 10, 7), "Tienda", "001")];
        await repo.ImportAsync("commerce_07102026.csv", rows, hash, default);
        var error = await Assert.ThrowsAsync<SqlException>(() => repo.ImportAsync("commerce_07102026.csv", rows, hash, default));
        Assert.Contains(error.Number, new[] { 2601, 2627 });
        var overview = await repo.GetOverviewAsync(default);
        Assert.Equal(1, overview.ImportCount); Assert.Equal(1, overview.CommerceCount);
    });

    [SqlFact]
    public Task ConcurrentProcessingMovesEachRowOnce() => WithDatabase(async repo =>
    {
        var date = new DateOnly(2026, 10, 7);
        var rows = Enumerable.Range(0, 100).Select(i => Row(date, "", i.ToString())).ToArray();
        await repo.ImportAsync("commerce_07102026.csv", rows, RandomNumberGenerator.GetBytes(32), default);
        var results = await Task.WhenAll(repo.ProcessAsync(date, default), repo.ProcessAsync(date, default));
        Assert.Equal(100, results.Sum(result => result.QuarantinedCount));
        var page = await repo.GetQuarantineAsync(date, 2, 10, default);
        Assert.Equal(100, page.TotalCount); Assert.Equal(10, page.Items.Count);
        Assert.Equal(10, page.Items.Select(row => row.Id).Distinct().Count());
    });

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Commerce.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
