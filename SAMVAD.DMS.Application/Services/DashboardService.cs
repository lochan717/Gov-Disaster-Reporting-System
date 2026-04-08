using SAMVAD.DMS.Application.DTOs.Dashboard;
using SAMVAD.DMS.Domain.Entities;
using SAMVAD.DMS.Domain.Enums;
using SAMVAD.DMS.Domain.Interfaces;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<Result<DashboardAnalyticsDto>> GetAnalyticsAsync(Guid? districtId, string userId, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds, CancellationToken cancellationToken = default)
    {
        var districtFilter = assignedDistrictIds.ToList();
        var incidents = _unitOfWork.Query<Incident>().AsQueryable();
        if (!isSuperAdmin)
        {
            incidents = incidents.Where(x => districtFilter.Contains(x.DistrictId));
        }

        if (districtId.HasValue)
        {
            incidents = incidents.Where(x => x.DistrictId == districtId.Value);
        }

        var incidentList = incidents.ToList();
        var totalIncidents = incidentList.Count;

        var avgResponseMinutes =
            (from incident in incidentList
             let start = _unitOfWork.Query<IncidentStatusHistory>().FirstOrDefault(x => x.IncidentId == incident.Id && x.ToStatus == IncidentStatus.InProgress)
             where start != null
             select (start.Timestamp - incident.CreatedAt).TotalMinutes)
            .DefaultIfEmpty(0)
            .Average();

        var avgResolutionMinutes =
            (from incident in incidentList
             where incident.ClosedAt.HasValue
             select ((incident.ClosedAt ?? incident.CreatedAt) - incident.CreatedAt).TotalMinutes)
            .DefaultIfEmpty(0)
            .Average();

        var payload = new DashboardAnalyticsDto
        {
            KpiCards =
            [
                new KpiCardDto { Key = "total", Title = "Total Incidents", Value = totalIncidents },
                new KpiCardDto { Key = "response", Title = "Avg Response Time", Value = Convert.ToDecimal(avgResponseMinutes), Unit = "min" },
                new KpiCardDto { Key = "resolution", Title = "Avg Resolution Time", Value = Convert.ToDecimal(avgResolutionMinutes), Unit = "min" }
            ],
            IncidentDistribution = incidentList
                .GroupBy(x => x.DisasterType)
                .Select(g => new IncidentDistributionDto { DisasterType = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToArray(),
            StatusBreakdown = incidentList
                .GroupBy(x => x.Status)
                .Select(g => new StatusBreakdownDto { Status = g.Key, Count = g.Count() })
                .OrderBy(x => x.Status)
                .ToArray()
        };

        return Task.FromResult(Result<DashboardAnalyticsDto>.Succeed(payload));
    }

    public Task<Result<CrossDistrictOverviewDto>> GetCrossDistrictOverviewAsync(string userId, bool isSuperAdmin, CancellationToken cancellationToken = default)
    {
        if (!isSuperAdmin)
        {
            return Task.FromResult(Result<CrossDistrictOverviewDto>.Fail("Access denied."));
        }

        var districts = _unitOfWork.Query<District>().ToList();
        var incidents = _unitOfWork.Query<Incident>().ToList();

        var payload = new CrossDistrictOverviewDto
        {
            Districts = districts
                .Select(d =>
                {
                    var districtIncidents = incidents.Where(i => i.DistrictId == d.Id).ToList();
                    return new DistrictOverviewItemDto
                    {
                        DistrictId = d.Id,
                        DistrictName = d.Name,
                        TotalIncidents = districtIncidents.Count,
                        OpenIncidents = districtIncidents.Count(i => i.Status == IncidentStatus.Open),
                        InProgressIncidents = districtIncidents.Count(i => i.Status == IncidentStatus.InProgress),
                        ClosedIncidents = districtIncidents.Count(i => i.Status == IncidentStatus.Closed)
                    };
                })
                .OrderBy(x => x.DistrictName)
                .ToArray()
        };

        return Task.FromResult(Result<CrossDistrictOverviewDto>.Succeed(payload));
    }
}