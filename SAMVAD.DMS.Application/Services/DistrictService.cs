using System.Text.Json;
using SAMVAD.DMS.Application.DTOs.District;
using SAMVAD.DMS.Domain.Entities;
using SAMVAD.DMS.Domain.Interfaces;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Application.Services;

public class DistrictService : IDistrictService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public DistrictService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public Task<Result<IReadOnlyCollection<DistrictPublicOptionDto>>> GetPublicActiveDistrictsAsync(CancellationToken cancellationToken = default)
    {
        var items = _unitOfWork.Query<District>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new DistrictPublicOptionDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                State = x.State,
                PublicReportUrl = x.PublicReportUrl
            })
            .ToArray();

        return Task.FromResult(Result<IReadOnlyCollection<DistrictPublicOptionDto>>.Succeed(items));
    }

    public async Task<Result<DistrictResponseDto>> CreateDistrictAsync(DistrictRequestDto request, string userId, CancellationToken cancellationToken = default)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var exists = _unitOfWork.Query<District>().Any(x => x.Code == normalizedCode);
        if (exists)
        {
            return Result<DistrictResponseDto>.Fail("District code already exists.");
        }

        var district = new District
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = normalizedCode,
            State = request.State.Trim(),
            IsActive = request.IsActive,
            CreatedOn = DateTime.UtcNow
        };

        await _unitOfWork.AddAsync(district, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAuditAsync(
            userId,
            "Create",
            nameof(District),
            district.Id.ToString(),
            null,
            SerializeDistrictAuditSnapshot(district),
            cancellationToken);

        return Result<DistrictResponseDto>.Succeed(MapToResponse(district));
    }

    public Task<Result<DistrictResponseDto>> GetDistrictByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var district = _unitOfWork.Query<District>().FirstOrDefault(x => x.Code == normalizedCode);
        return Task.FromResult(district is null
            ? Result<DistrictResponseDto>.Fail("District not found.")
            : Result<DistrictResponseDto>.Succeed(MapToResponse(district)));
    }

    public Task<Result<DistrictResponseDto>> GetDistrictByIdAsync(Guid districtId, CancellationToken cancellationToken = default)
    {
        var district = _unitOfWork.Query<District>().FirstOrDefault(x => x.Id == districtId);
        return Task.FromResult(district is null
            ? Result<DistrictResponseDto>.Fail("District not found.")
            : Result<DistrictResponseDto>.Succeed(MapToResponse(district)));
    }

    public Task<Result<PaginatedResult<DistrictListItemDto>>> GetDistrictsAsync(int page, int pageSize, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var currentPage = page < 1 ? 1 : page;
        var currentPageSize = pageSize < 1 ? 20 : pageSize;

        var districtQuery = _unitOfWork.Query<District>();
        var userDistricts = _unitOfWork.Query<ApplicationUserDistrict>();
        var incidents = _unitOfWork.Query<Incident>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            districtQuery = districtQuery.Where(x =>
                x.Name.ToLower().Contains(term) ||
                x.Code.ToLower().Contains(term) ||
                x.State.ToLower().Contains(term));
        }

        var totalCount = districtQuery.Count();
        var items = districtQuery
            .OrderBy(x => x.Name)
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .Select(x => new DistrictListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                State = x.State,
                IsActive = x.IsActive,
                ActiveAdminCount = userDistricts.Count(ud => ud.DistrictId == x.Id),
                TotalIncidents = incidents.Count(i => i.DistrictId == x.Id)
            })
            .ToArray();

        return Task.FromResult(Result<PaginatedResult<DistrictListItemDto>>.Succeed(
            PaginatedResult<DistrictListItemDto>.Create(items, totalCount, currentPage, currentPageSize)));
    }

    public async Task<Result<bool>> ToggleDistrictStatusAsync(Guid districtId, string userId, CancellationToken cancellationToken = default)
    {
        var district = _unitOfWork.Query<District>().FirstOrDefault(x => x.Id == districtId);
        if (district is null)
        {
            return Result<bool>.Fail("District not found.");
        }

        var oldValues = SerializeDistrictAuditSnapshot(district);
        district.IsActive = !district.IsActive;
        _unitOfWork.Update(district);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAuditAsync(
            userId,
            "ToggleStatus",
            nameof(District),
            district.Id.ToString(),
            oldValues,
            SerializeDistrictAuditSnapshot(district),
            cancellationToken);

        return Result<bool>.Succeed(true, "District status updated.");
    }

    public async Task<Result<DistrictResponseDto>> UpdateDistrictAsync(Guid districtId, DistrictRequestDto request, string userId, CancellationToken cancellationToken = default)
    {
        var district = _unitOfWork.Query<District>().FirstOrDefault(x => x.Id == districtId);
        if (district is null)
        {
            return Result<DistrictResponseDto>.Fail("District not found.");
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var codeExists = _unitOfWork.Query<District>().Any(x => x.Code == normalizedCode && x.Id != districtId);
        if (codeExists)
        {
            return Result<DistrictResponseDto>.Fail("District code already exists.");
        }

        var oldValues = SerializeDistrictAuditSnapshot(district);
        district.Name = request.Name.Trim();
        district.Code = normalizedCode;
        district.State = request.State.Trim();
        district.IsActive = request.IsActive;

        _unitOfWork.Update(district);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAuditAsync(
            userId,
            "Update",
            nameof(District),
            district.Id.ToString(),
            oldValues,
            SerializeDistrictAuditSnapshot(district),
            cancellationToken);

        return Result<DistrictResponseDto>.Succeed(MapToResponse(district));
    }

    public async Task<Result<bool>> DeleteDistrictAsync(Guid districtId, string userId, CancellationToken cancellationToken = default)
    {
        var district = _unitOfWork.Query<District>().FirstOrDefault(x => x.Id == districtId);
        if (district is null)
        {
            return Result<bool>.Fail("District not found.");
        }

        var hasAssignedAdmins = _unitOfWork.Query<ApplicationUserDistrict>().Any(x => x.DistrictId == districtId);
        if (hasAssignedAdmins)
        {
            return Result<bool>.Fail("Cannot delete district because admins are assigned to it.");
        }

        var hasIncidents = _unitOfWork.Query<Incident>().Any(x => x.DistrictId == districtId);
        if (hasIncidents)
        {
            return Result<bool>.Fail("Cannot delete district because incidents are linked to it.");
        }

        var oldValues = SerializeDistrictAuditSnapshot(district);
        _unitOfWork.Remove(district);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAuditAsync(
            userId,
            "Delete",
            nameof(District),
            district.Id.ToString(),
            oldValues,
            null,
            cancellationToken);

        return Result<bool>.Succeed(true, "District deleted successfully.");
    }

    private static DistrictResponseDto MapToResponse(District district)
    {
        return new DistrictResponseDto
        {
            Id = district.Id,
            Name = district.Name,
            Code = district.Code,
            State = district.State,
            IsActive = district.IsActive,
            CreatedOn = district.CreatedOn,
            PublicReportUrl = district.PublicReportUrl
        };
    }

    private static string SerializeDistrictAuditSnapshot(District district)
    {
        var snapshot = new
        {
            district.Id,
            district.Name,
            district.Code,
            district.State,
            district.IsActive,
            district.CreatedOn,
            district.PublicReportUrl
        };

        return JsonSerializer.Serialize(snapshot);
    }
}