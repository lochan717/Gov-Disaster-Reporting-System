using System.ComponentModel.DataAnnotations;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class IncidentUploadFileDto
{
    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string ContentType { get; set; } = string.Empty;
}