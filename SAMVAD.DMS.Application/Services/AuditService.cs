using SAMVAD.DMS.Domain.Entities;
using SAMVAD.DMS.Domain.Interfaces;
using SAMVAD.DMS.Application.DTOs.Audit;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<Result<PaginatedResult<AuditLogDto>>> GetAuditLogsAsync(AuditFilterDto filter, CancellationToken cancellationToken = default)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;

        var query = _unitOfWork.Query<AuditLog>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.UserId))
        {
            query = query.Where(x => x.UserId == filter.UserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(x => x.Action == filter.Action);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(x => x.EntityType == filter.EntityType);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(x => x.Timestamp >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(x => x.Timestamp <= filter.ToDate.Value);
        }

        var totalCount = query.Count();
        var items = query
            .OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                OldValues = x.OldValues,
                NewValues = x.NewValues,
                Timestamp = x.Timestamp
            })
            .ToArray();

        var payload = PaginatedResult<AuditLogDto>.Create(items, totalCount, page, pageSize);
        return Task.FromResult(Result<PaginatedResult<AuditLogDto>>.Succeed(payload));
    }

    public async Task LogAuditAsync(string userId, string action, string entityType, string entityId, string? oldValues, string? newValues, CancellationToken cancellationToken = default)
    {
        var audit = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            Timestamp = DateTime.UtcNow
        };

        await _unitOfWork.AddAsync(audit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}