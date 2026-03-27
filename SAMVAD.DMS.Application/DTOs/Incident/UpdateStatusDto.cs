using System.ComponentModel.DataAnnotations;
using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class UpdateStatusDto
{
    [Required]
    public IncidentStatus NewStatus { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}