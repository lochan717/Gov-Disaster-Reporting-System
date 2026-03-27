namespace SAMVAD.DMS.Application.DTOs.User;

public class AdminDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastLogin { get; set; }
    public IReadOnlyCollection<Guid> AssignedDistrictIds { get; set; } = Array.Empty<Guid>();
}