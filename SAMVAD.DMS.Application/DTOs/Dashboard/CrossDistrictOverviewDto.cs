namespace SAMVAD.DMS.Application.DTOs.Dashboard;

public class CrossDistrictOverviewDto
{
    public IReadOnlyCollection<DistrictOverviewItemDto> Districts { get; set; } = Array.Empty<DistrictOverviewItemDto>();
}

public class DistrictOverviewItemDto
{
    public Guid DistrictId { get; set; }
    public string DistrictName { get; set; } = string.Empty;
    public int TotalIncidents { get; set; }
    public int OpenIncidents { get; set; }
    public int InProgressIncidents { get; set; }
    public int ClosedIncidents { get; set; }
}