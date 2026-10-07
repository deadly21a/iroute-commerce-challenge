using System.ComponentModel.DataAnnotations;

namespace Commerce.Api.Models;

public sealed record CommerceRow(DateOnly PcProcessdate, string PcNomcomred, string PcNumdoc,
    string PcEmail, string PcTelefono, string PcDireccion);
public sealed record ImportResult(Guid BatchId, string FileName, int InsertedCount, DateTime ImportedAt);
public sealed record ProcessRequest([Required] DateOnly? ProcessDate);
public sealed record ProcessResult(DateOnly ProcessDate, int QuarantinedCount, int RemainingCount);
public sealed record QuarantineRow(long Id, Guid BatchId, DateOnly PcProcessdate, string PcNomcomred,
    string PcNumdoc, string PcEmail, string PcTelefono, string PcDireccion, string Motivo, DateTime QuarantinedAt);
public sealed record PageResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
public sealed record DateSummary(DateOnly ProcessDate, int PendingCount, int QuarantinedCount);
public sealed record BatchSummary(Guid BatchId, string FileName, int RowCount, DateTime ImportedAt);
public sealed record Overview(int CommerceCount, int QuarantineCount, int ImportCount,
    IReadOnlyList<DateSummary> Dates, IReadOnlyList<BatchSummary> RecentImports);
public sealed record LoginRequest([Required, EmailAddress] string Email,
    [Required, MinLength(1), MaxLength(200)] string Password);
public sealed record UserInfo(string Email, string Name);
public sealed record LoginResult(string Token, DateTime ExpiresAt, UserInfo User);
public sealed record AppUser(long Id, string Email, string Name, string PasswordHash);
