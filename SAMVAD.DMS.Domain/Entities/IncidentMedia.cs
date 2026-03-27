using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Domain.Entities;

public class IncidentMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncidentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public IncidentMediaType MediaType { get; set; }
    public MediaUploadedBy UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public virtual Incident? Incident { get; set; }
}