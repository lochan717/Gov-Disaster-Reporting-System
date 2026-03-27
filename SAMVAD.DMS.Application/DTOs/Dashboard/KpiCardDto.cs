namespace SAMVAD.DMS.Application.DTOs.Dashboard;

public class KpiCardDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Unit { get; set; }
}