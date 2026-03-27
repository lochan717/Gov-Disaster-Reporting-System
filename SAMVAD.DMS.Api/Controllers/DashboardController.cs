using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.DTOs.Dashboard;
using SAMVAD.DMS.Application.Services;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminAccess")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<ApiResponseDto<DashboardAnalyticsDto>>> GetAnalytics([FromQuery] Guid? districtId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var assignedDistrictIds = GetAssignedDistrictIds();
        var result = await _dashboardService.GetAnalyticsAsync(districtId, userId, isSuperAdmin, assignedDistrictIds);
        return result.IsSuccess
            ? Ok(ApiResponseDto<DashboardAnalyticsDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<DashboardAnalyticsDto>.Error(result.Message ?? "Failed."));
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpGet("cross-district")]
    public async Task<ActionResult<ApiResponseDto<CrossDistrictOverviewDto>>> GetCrossDistrict()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _dashboardService.GetCrossDistrictOverviewAsync(userId, true);
        return result.IsSuccess
            ? Ok(ApiResponseDto<CrossDistrictOverviewDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<CrossDistrictOverviewDto>.Error(result.Message ?? "Failed."));
    }

    private IReadOnlyCollection<Guid> GetAssignedDistrictIds()
    {
        var raw = User.FindFirstValue("AssignedDistrictIds");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<Guid>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();
    }
}