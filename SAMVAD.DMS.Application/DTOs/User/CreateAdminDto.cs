using System.ComponentModel.DataAnnotations;

namespace SAMVAD.DMS.Application.DTOs.User;

public class CreateAdminDto
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string RoleLabel { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<Guid> AssignedDistrictIds { get; set; } = new();
}