using Microsoft.AspNetCore.Identity;

namespace SAMVAD.DMS.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public DateTime? PasswordResetRequestedAt { get; set; }

    public virtual ICollection<ApplicationUserDistrict> AssignedDistricts { get; set; } =
        new List<ApplicationUserDistrict>();
}