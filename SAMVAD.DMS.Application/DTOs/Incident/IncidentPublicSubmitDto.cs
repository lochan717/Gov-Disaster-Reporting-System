using System.ComponentModel.DataAnnotations;
using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class IncidentPublicSubmitDto
{
    [Required]
    public Guid? DistrictId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public DisasterType DisasterType { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public IncidentPriority Priority { get; set; }

    [Range(-90, 90)]
    public decimal? LocationGpsLat { get; set; }

    [Range(-180, 180)]
    public decimal? LocationGpsLng { get; set; }

    [StringLength(300)]
    public string? LocationText { get; set; }

    [StringLength(1000)]
    public string? Details { get; set; }

    [Required]
    [RegularExpression("^[0-9]{10}$")]
    public string MobileNumber { get; set; } = string.Empty;
}