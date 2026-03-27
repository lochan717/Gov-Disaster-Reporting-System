using System.ComponentModel.DataAnnotations;

namespace SAMVAD.DMS.Application.DTOs.User;

public class UpdateProfileDto
{
    [Required]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [RegularExpression("^[0-9]{10}$")]
    public string? ContactNumber { get; set; }
}