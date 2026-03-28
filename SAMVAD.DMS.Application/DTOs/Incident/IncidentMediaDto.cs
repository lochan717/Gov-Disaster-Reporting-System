using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class IncidentMediaDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public IncidentMediaType MediaType { get; set; }
    public MediaUploadedBy UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}