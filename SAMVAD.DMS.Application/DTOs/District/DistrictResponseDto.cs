namespace SAMVAD.DMS.Application.DTOs.District;

public class DistrictResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public string PublicReportUrl { get; set; } = string.Empty;
}