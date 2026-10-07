using Commerce.Api.Data;
using Commerce.Api.Models;
using Commerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Commerce.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/commerce")]
public sealed class CommerceController(CommerceRepository repository, CsvImportService parser) : ControllerBase
{
    [HttpPost("import")]
    [RequestSizeLimit(CsvImportService.MaxBytes + 65536)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImportResult>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Problem(statusCode: 400, title: "Archivo inválido", detail: "Selecciona un CSV que no esté vacío.");
        if (file.Length > CsvImportService.MaxBytes)
            return Problem(statusCode: 400, title: "Archivo inválido", detail: "El archivo supera el límite de 10 MB.");
        try
        {
            await using var stream = file.OpenReadStream();
            var parsed = await parser.ParseAsync(stream, file.FileName, ct);
            var result = await repository.ImportAsync(file.FileName, parsed.Rows, parsed.Hash, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ImportValidationException ex) { return Problem(statusCode: 400, title: "Archivo inválido", detail: ex.Message); }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        { return Problem(statusCode: 409, title: "Archivo ya importado", detail: "Este contenido ya fue importado. No se duplicaron registros."); }
    }

    [HttpPost("process")]
    public async Task<ActionResult<ProcessResult>> Process(ProcessRequest request, CancellationToken ct)
    {
        if (request.ProcessDate is null || request.ProcessDate == DateOnly.MinValue)
            return Problem(statusCode: 400, title: "Fecha inválida", detail: "Indica processDate con formato yyyy-MM-dd.");
        return Ok(await repository.ProcessAsync(request.ProcessDate.Value, ct));
    }

    [HttpGet("quarantine")]
    public async Task<ActionResult<PageResult<QuarantineRow>>> Quarantine([FromQuery] DateOnly? processDate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        if (page < 1 || page > 1_000_000 || pageSize is < 1 or > 100)
            return Problem(statusCode: 400, title: "Paginación inválida", detail: "page debe estar entre 1 y 1000000; pageSize entre 1 y 100.");
        return Ok(await repository.GetQuarantineAsync(processDate, page, pageSize, ct));
    }

    [HttpGet("overview")]
    public async Task<ActionResult<Overview>> Overview(CancellationToken ct) => Ok(await repository.GetOverviewAsync(ct));
}
