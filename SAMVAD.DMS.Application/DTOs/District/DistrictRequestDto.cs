using System.ComponentModel.DataAnnotations;

namespace SAMVAD.DMS.Application.DTOs.District;

public class DistrictRequestDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    [RegularExpression("^[A-Z0-9-]+$")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string State { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}