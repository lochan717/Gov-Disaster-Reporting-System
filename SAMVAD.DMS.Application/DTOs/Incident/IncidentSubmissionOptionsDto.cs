namespace SAMVAD.DMS.Application.DTOs.Incident;

public class IncidentSubmissionOptionsDto
{
    public IReadOnlyCollection<IncidentSubmissionDistrictOptionDto> Districts { get; set; } = Array.Empty<IncidentSubmissionDistrictOptionDto>();
    public IReadOnlyCollection<string> Channels { get; set; } = Array.Empty<string>();
}