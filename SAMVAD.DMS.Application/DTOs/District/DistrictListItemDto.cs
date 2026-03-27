namespace SAMVAD.DMS.Application.DTOs.District;

public class DistrictListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ActiveAdminCount { get; set; }
    public int TotalIncidents { get; set; }
}