using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class IncidentListItemDto
{
    public Guid Id { get; set; }
    public string IncidentId { get; set; } = string.Empty;
    public Guid DistrictId { get; set; }
    public string DistrictName { get; set; } = string.Empty;
    public DisasterType DisasterType { get; set; }
    public IncidentPriority Priority { get; set; }
    public string LocationText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public IncidentStatus Status { get; set; }
}