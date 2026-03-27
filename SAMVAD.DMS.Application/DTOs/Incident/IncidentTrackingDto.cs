using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class IncidentTrackingDto
{
    public string IncidentId { get; set; } = string.Empty;
    public string TrackingToken { get; set; } = string.Empty;
    public DisasterType DisasterType { get; set; }
    public IncidentStatus Status { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}