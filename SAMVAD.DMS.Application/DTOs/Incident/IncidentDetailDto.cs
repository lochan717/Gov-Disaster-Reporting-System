using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class IncidentDetailDto
{
    public Guid Id { get; set; }
    public string IncidentId { get; set; } = string.Empty;
    public Guid DistrictId { get; set; }
    public string DistrictName { get; set; } = string.Empty;
    public DisasterType DisasterType { get; set; }
    public IncidentPriority Priority { get; set; }
    public decimal? LocationGpsLat { get; set; }
    public decimal? LocationGpsLng { get; set; }
    public string LocationText { get; set; } = string.Empty;
    public string ReporterMobile { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public IReadOnlyCollection<IncidentStatusHistoryDto> StatusHistory { get; set; } = Array.Empty<IncidentStatusHistoryDto>();
    public IReadOnlyCollection<IncidentCommentDto> Comments { get; set; } = Array.Empty<IncidentCommentDto>();
    public IReadOnlyCollection<IncidentMediaDto> MediaFiles { get; set; } = Array.Empty<IncidentMediaDto>();
}