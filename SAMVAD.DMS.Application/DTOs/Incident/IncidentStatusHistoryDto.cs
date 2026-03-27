using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class IncidentStatusHistoryDto
{
    public IncidentStatus FromStatus { get; set; }
    public IncidentStatus ToStatus { get; set; }
    public string ChangedById { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}