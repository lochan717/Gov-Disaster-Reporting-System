using System.ComponentModel.DataAnnotations;

namespace SAMVAD.DMS.Application.DTOs.User;

public class ResetUserPasswordDto
{
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Confirm password does not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}