using System.Text;
using Commerce.Api.Services;

namespace Commerce.Tests;

public sealed class CsvImportTests
{
    private const string Header = "pc_processdate,pc_nomcomred,pc_numdoc,pc_email,pc_telefono,pc_direccion\n";
    private readonly CsvImportService parser = new();
    private async Task<ParsedImport> Parse(string text, string name = "commerce_07102026.csv")
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return await parser.ParseAsync(stream, name, CancellationToken.None);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(Header)]
    public async Task RejectsEmptyOrHeaderOnly(string content) => await Assert.ThrowsAsync<ImportValidationException>(() => Parse(content));

    [Theory]
    [InlineData("commerce_31022026.csv")]
    [InlineData("commerce_07102026.txt")]
    [InlineData("../commerce_07102026.csv")]
    [InlineData("commerce.csv")]
    public async Task RejectsInvalidNames(string name) => await Assert.ThrowsAsync<ImportValidationException>(() => Parse(Header + "2026-10-07,Tienda,0012,,,", name));

    [Fact]
    public async Task PreservesZerosAndQuotedMultilineFields()
    {
        var result = await Parse("\uFEFF" + Header + "2026-10-07,\"Café, Sur\",00123,,,\"Av. Uno\nLocal 2\"\n");
        var row = Assert.Single(result.Rows);
        Assert.Equal("00123", row.PcNumdoc);
        Assert.Equal("Café, Sur", row.PcNomcomred);
        Assert.Equal("Av. Uno\nLocal 2", row.PcDireccion);
    }

    [Fact]
    public async Task ImportsBusinessErrorsForLaterSqlProcessing()
    {
        var result = await Parse(Header + "2026-10-07,,AB-12,,,\n2026-10-08,Otro,,,,\n");
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("AB-12", result.Rows[0].PcNumdoc);
        Assert.Equal("", result.Rows[0].PcNomcomred);
        Assert.Equal(new DateOnly(2026, 10, 8), result.Rows[1].PcProcessdate);
    }

    [Theory]
    [InlineData("07/10/2026,Tienda,123,,,")]
    [InlineData("2026-02-30,Tienda,123,,,")]
    [InlineData("2026-10-07,Tienda,123,,")]
    [InlineData("2026-10-07,Tienda,123,,,,extra")]
    [InlineData("2026-10-07,\"sin cerrar,123,,,")]
    public async Task RejectsMalformedRows(string row) => await Assert.ThrowsAsync<ImportValidationException>(() => Parse(Header + row));

    [Fact]
    public async Task RejectsChangedHeaders() => await Assert.ThrowsAsync<ImportValidationException>(() => Parse(Header.Replace("pc_numdoc", "documento") + "2026-10-07,Tienda,123,,,"));

    [Fact]
    public async Task RejectsOversizedFields() => await Assert.ThrowsAsync<ImportValidationException>(() => Parse(Header + "2026-10-07," + new string('A', 201) + ",123,,,"));

    [Fact]
    public async Task RejectsInvalidUtf8()
    {
        using var stream = new MemoryStream([0xC3, 0x28]);
        await Assert.ThrowsAsync<ImportValidationException>(() => parser.ParseAsync(stream, "commerce_07102026.csv", CancellationToken.None));
    }
}
