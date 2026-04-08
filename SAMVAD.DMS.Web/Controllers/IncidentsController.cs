using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.DTOs.Incident;
using SAMVAD.DMS.Shared.Models;
using SAMVAD.DMS.Web.Services;

namespace SAMVAD.DMS.Web.Controllers;

[Authorize(Policy = "AdminAccess")]
public class IncidentsController : Controller
{
    private readonly IHttpService _httpService;

    public IncidentsController(IHttpService httpService)
    {
        _httpService = httpService;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> List([FromBody] IncidentFilterDto filter)
    {
        filter ??= new IncidentFilterDto();
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid incident filter." : firstError });
        }

        var response = await _httpService.PostAsync<ApiResponseDto<PaginatedResult<IncidentListItemDto>>>("api/incidents/list", filter);
        var payload = response?.Data ?? PaginatedResult<IncidentListItemDto>.Create(Array.Empty<IncidentListItemDto>(), 0, filter.Page, filter.PageSize);
        return PartialView("_List", payload);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var response = await _httpService.GetAsync<ApiResponseDto<IncidentDetailDto>>($"api/incidents/{id}");
        return PartialView("_Detail", response?.Data);
    }

    [HttpGet]
    public async Task<IActionResult> SubmitOnBehalfForm()
    {
        var optionsResponse = await _httpService.GetAsync<ApiResponseDto<IncidentSubmissionOptionsDto>>("api/incidents/submission-options");
        ViewBag.SubmissionDistrictOptions = optionsResponse?.Data?.Districts ?? Array.Empty<IncidentSubmissionDistrictOptionDto>();
        ViewBag.SubmissionChannels = optionsResponse?.Data?.Channels ?? Array.Empty<string>();
        return PartialView("_SubmitOnBehalf", new IncidentAdminSubmitDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitOnBehalf(IncidentAdminSubmitDto model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid incident data." : firstError });
        }

        var response = await _httpService.PostAsync<ApiResponseDto<IncidentTrackingDto>>("api/incidents/on-behalf", model);
        return Json(new
        {
            success = response?.Success == true,
            message = response?.Message ?? (response?.Success == true ? "Incident submitted successfully." : "Failed to submit incident."),
            data = response?.Data
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateStatusDto model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid status update request." : firstError });
        }

        try
        {
            var response = await _httpService.PatchAsync<ApiResponseDto<bool>>($"api/incidents/{id}/status", model);
            if (response is null)
            {
                return Json(new { success = false, message = "No response received from API." });
            }

            return Json(new
            {
                success = response.Success,
                message = response.Message ?? (response.Success ? "Status updated." : "Failed to update status.")
            });
        }
        catch
        {
            return Json(new { success = false, message = "Failed to update status." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, AddCommentDto model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid comment request." : firstError });
        }

        var response = await _httpService.PostAsync<ApiResponseDto<IncidentCommentDto>>($"api/incidents/{id}/comments", model);
        return Json(new { success = response?.Success == true, message = response?.Message, data = response?.Data });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var payload = new IncidentFilterDto();
        var bytes = await _httpService.PostBytesAsync("api/incidents/export-csv", payload);
        if (bytes is null || bytes.Length == 0)
        {
            TempData["Error"] = "Unable to export incidents.";
            return RedirectToAction(nameof(Index));
        }

        var fileName = $"DMS_Export_ALL_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    [HttpGet]
    public Task<IActionResult> ExportCsv()
    {
        return Task.FromResult<IActionResult>(RedirectToAction(nameof(Index))!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportCsv(IncidentFilterDto filter)
    {
        filter ??= new IncidentFilterDto();
        if (!TryValidateModel(filter))
        {
            TempData["Error"] = "Invalid export filters.";
            return RedirectToAction(nameof(Index));
        }

        var bytes = await _httpService.PostBytesAsync("api/incidents/export-csv", filter);
        if (bytes is null || bytes.Length == 0)
        {
            TempData["Error"] = "Unable to export incidents.";
            return RedirectToAction(nameof(Index));
        }

        var districtSegment = filter.DistrictId.HasValue ? filter.DistrictId.Value.ToString("N") : "FILTERED";
        var fileName = $"DMS_Export_{districtSegment}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        var bytes = await _httpService.GetBytesAsync($"api/incidents/{id}/export-pdf");
        if (bytes is null || bytes.Length == 0)
        {
            TempData["Error"] = "Unable to export incident PDF.";
            return RedirectToAction(nameof(Index));
        }

        var fileName = $"Incident_Report_{id:N}_{DateTime.UtcNow:yyyyMMdd}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadMedia(Guid incidentId, Guid mediaId, string? fileName = null, string? contentType = null, bool download = false)
    {
        var bytes = await _httpService.GetBytesAsync($"api/incidents/{incidentId}/media/{mediaId}");
        if (bytes is null || bytes.Length == 0)
        {
            TempData["Error"] = "Unable to load media file.";
            return RedirectToAction(nameof(Index));
        }

        var safeContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
        var safeFileName = string.IsNullOrWhiteSpace(fileName) ? $"attachment_{mediaId:N}" : fileName;

        if (download)
        {
            return File(bytes, safeContentType, safeFileName);
        }

        return File(bytes, safeContentType);
    }
}