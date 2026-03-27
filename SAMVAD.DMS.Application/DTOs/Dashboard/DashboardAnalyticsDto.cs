namespace SAMVAD.DMS.Application.DTOs.Dashboard;

public class DashboardAnalyticsDto
{
    public IReadOnlyCollection<KpiCardDto> KpiCards { get; set; } = Array.Empty<KpiCardDto>();
    public IReadOnlyCollection<IncidentDistributionDto> IncidentDistribution { get; set; } = Array.Empty<IncidentDistributionDto>();
    public IReadOnlyCollection<StatusBreakdownDto> StatusBreakdown { get; set; } = Array.Empty<StatusBreakdownDto>();
}