using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Dashboard;

public class IncidentDistributionDto
{
    public DisasterType DisasterType { get; set; }
    public int Count { get; set; }
}