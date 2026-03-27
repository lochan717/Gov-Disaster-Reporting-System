using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Dashboard;

public class StatusBreakdownDto
{
    public IncidentStatus Status { get; set; }
    public int Count { get; set; }
}