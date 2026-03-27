namespace SAMVAD.DMS.Application.DTOs.User;

public class AdminFormOptionsDto
{
    public IReadOnlyCollection<string> RoleOptions { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<AdminDistrictOptionDto> DistrictOptions { get; set; } = Array.Empty<AdminDistrictOptionDto>();
}

public class AdminDistrictOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}