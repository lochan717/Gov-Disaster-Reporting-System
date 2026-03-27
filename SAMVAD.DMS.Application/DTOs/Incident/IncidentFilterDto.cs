using System.ComponentModel.DataAnnotations;
using SAMVAD.DMS.Domain.Enums;

namespace SAMVAD.DMS.Application.DTOs.Incident;

public class IncidentFilterDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 20;

    public Guid? DistrictId { get; set; }
    public IncidentStatus? Status { get; set; }
    public DisasterType? DisasterType { get; set; }
    public IncidentPriority? Priority { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "desc";
}