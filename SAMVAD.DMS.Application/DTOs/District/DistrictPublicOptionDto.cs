namespace SAMVAD.DMS.Application.DTOs.District;

public class DistrictPublicOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PublicReportUrl { get; set; } = string.Empty;
}