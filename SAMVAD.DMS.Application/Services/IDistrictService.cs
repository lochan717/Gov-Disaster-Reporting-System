using SAMVAD.DMS.Application.DTOs.District;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Application.Services;

public interface IDistrictService
{
    Task<Result<IReadOnlyCollection<DistrictPublicOptionDto>>> GetPublicActiveDistrictsAsync(CancellationToken cancellationToken = default);
    Task<Result<PaginatedResult<DistrictListItemDto>>> GetDistrictsAsync(int page, int pageSize, string? searchTerm, CancellationToken cancellationToken = default);
    Task<Result<DistrictResponseDto>> GetDistrictByIdAsync(Guid districtId, CancellationToken cancellationToken = default);
    Task<Result<DistrictResponseDto>> GetDistrictByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Result<DistrictResponseDto>> CreateDistrictAsync(DistrictRequestDto request, string userId, CancellationToken cancellationToken = default);
    Task<Result<DistrictResponseDto>> UpdateDistrictAsync(Guid districtId, DistrictRequestDto request, string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ToggleDistrictStatusAsync(Guid districtId, string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteDistrictAsync(Guid districtId, string userId, CancellationToken cancellationToken = default);
}