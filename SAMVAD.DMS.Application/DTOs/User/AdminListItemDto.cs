namespace SAMVAD.DMS.Application.DTOs.User;

public class AdminListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public IReadOnlyCollection<string> AssignedDistricts { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
}