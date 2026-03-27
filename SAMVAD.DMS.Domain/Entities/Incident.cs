using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Domain.Entities;

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IncidentId { get; set; } = string.Empty;
    public Guid DistrictId { get; set; }
    public DisasterType DisasterType { get; set; }
    public IncidentPriority Priority { get; set; }
    public decimal? LocationGpsLat { get; set; }
    public decimal? LocationGpsLng { get; set; }
    public string LocationText { get; set; } = string.Empty;
    public string ReporterMobile { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public string? ResolutionNote { get; set; }
    public string TrackingToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public virtual District? District { get; set; }
    public virtual ICollection<IncidentStatusHistory> StatusHistory { get; set; } =
        new List<IncidentStatusHistory>();

    public virtual ICollection<IncidentComment> Comments { get; set; } = new List<IncidentComment>();
    public virtual ICollection<IncidentMedia> MediaFiles { get; set; } = new List<IncidentMedia>();
}