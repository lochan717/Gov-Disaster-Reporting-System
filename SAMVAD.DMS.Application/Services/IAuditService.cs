using SAMVAD.DMS.Application.DTOs.Audit;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Application.Services;

public interface IAuditService
{
    Task LogAuditAsync(
        string userId,
        string action,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        CancellationToken cancellationToken = default);

    Task<Result<PaginatedResult<AuditLogDto>>> GetAuditLogsAsync(AuditFilterDto filter, CancellationToken cancellationToken = default);
}