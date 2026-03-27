using System.ComponentModel.DataAnnotations;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class AddCommentDto
{
    [Required]
    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;
}