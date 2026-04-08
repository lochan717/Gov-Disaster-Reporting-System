using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.DTOs.Dashboard;
using SAMVAD.DMS.Application.DTOs.Incident;
using SAMVAD.DMS.Shared.Models;
using SAMVAD.DMS.Web.Services;

namespace SAMVAD.DMS.Web.Controllers;

[Authorize(Policy = "AdminAccess")]
public class DashboardController : Controller
{
    private readonly IHttpService _httpService;

    public DashboardController(IHttpService httpService)
    {
        _httpService = httpService;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analytics(Guid? districtId)
    {
        var response = await _httpService.GetAsync<ApiResponseDto<DashboardAnalyticsDto>>($"api/dashboard/analytics?districtId={districtId}");
        return Json(new { success = response?.Success == true, data = response?.Data, message = response?.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LiveFeed([FromBody] IncidentFilterDto filter)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid live feed filter." : firstError });
        }

        var response = await _httpService.PostAsync<ApiResponseDto<PaginatedResult<IncidentListItemDto>>>("api/incidents/list", filter);
        return Json(new { success = response?.Success == true, data = response?.Data, message = response?.Message });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var response = await _httpService.GetAsync<ApiResponseDto<IncidentDetailDto>>($"api/incidents/{id}");
        return PartialView("~/Views/Incidents/_Detail.cshtml", response?.Data);
    }
}