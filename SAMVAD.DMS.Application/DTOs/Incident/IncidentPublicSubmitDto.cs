using System.ComponentModel.DataAnnotations;
using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class IncidentPublicSubmitDto : IValidatableObject
{
    [Required(ErrorMessage = "District is required.")]
    public Guid? DistrictId { get; set; }

    [Required(ErrorMessage = "Disaster type is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Disaster type is required.")]
    public DisasterType DisasterType { get; set; }

    [Required(ErrorMessage = "Priority is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Priority is required.")]
    public IncidentPriority Priority { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? LocationGpsLat { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? LocationGpsLng { get; set; }

    [StringLength(300, ErrorMessage = "Location text cannot exceed 300 characters.")]
    public string? LocationText { get; set; }

    [StringLength(1000, ErrorMessage = "Details cannot exceed 1000 characters.")]
    public string? Details { get; set; }

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression("^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit mobile number.")]
    public string MobileNumber { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasGps = LocationGpsLat.HasValue && LocationGpsLng.HasValue;
        var hasLocationText = !string.IsNullOrWhiteSpace(LocationText);

        if (!hasGps && !hasLocationText)
        {
            yield return new ValidationResult(
                "Location text is required when GPS is not captured.",
                new[] { nameof(LocationText) });
        }
    }
}