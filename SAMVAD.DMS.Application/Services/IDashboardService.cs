using SAMVAD.DMS.Application.DTOs.Dashboard;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Application.Services;

public interface IDashboardService
{
    Task<Result<DashboardAnalyticsDto>> GetAnalyticsAsync(
        Guid? districtId,
        string userId,
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<CrossDistrictOverviewDto>> GetCrossDistrictOverviewAsync(
        string userId,
        bool isSuperAdmin,
        CancellationToken cancellationToken = default);
}