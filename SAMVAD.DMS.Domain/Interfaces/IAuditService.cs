namespace SAMVAD.DMS.Domain.Interfaces;

public interface IAuditService
{
    Task LogAuditAsync(
        string userId,
        string action,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues);
}