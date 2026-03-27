using System.ComponentModel.DataAnnotations.Schema;

namespace SAMVAD.DMS.Domain.Entities;

public class District
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string PublicReportUrl => $"/report/{Code.ToLowerInvariant()}";

    public virtual ICollection<ApplicationUserDistrict> AssignedAdmins { get; set; } =
        new List<ApplicationUserDistrict>();

    public virtual ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}