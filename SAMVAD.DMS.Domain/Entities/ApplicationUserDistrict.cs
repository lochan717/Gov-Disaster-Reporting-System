namespace SAMVAD.DMS.Domain.Entities;

public class ApplicationUserDistrict
{
    public string ApplicationUserId { get; set; } = string.Empty;
    public Guid DistrictId { get; set; }

    public virtual ApplicationUser? ApplicationUser { get; set; }
    public virtual District? District { get; set; }
}