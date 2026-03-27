using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SAMVAD.DMS.Application.DTOs.Incident;
using SAMVAD.DMS.Shared.Models;
using SAMVAD.DMS.Web.Services;

namespace SAMVAD.DMS.Web.Controllers;

public class PublicController : Controller
{
    private readonly IHttpService _httpService;

    public PublicController(IHttpService httpService)
    {
        _httpService = httpService;
    }

    [HttpGet("")]
    [HttpGet("landing")]
    public async Task<IActionResult> Landing()
    {
        var response = await _httpService.GetAsync<ApiResponseDto<IReadOnlyCollection<SAMVAD.DMS.Application.DTOs.District.DistrictPublicOptionDto>>>("api/districts/public");
        var districts = response?.Data ?? Array.Empty<SAMVAD.DMS.Application.DTOs.District.DistrictPublicOptionDto>();
        return View(districts);
    }

    [HttpGet("report")]
    [HttpGet("report/{districtCode}")]
    public async Task<IActionResult> Report(string? districtCode = null)
    {
        var response = await _httpService.GetAsync<ApiResponseDto<IReadOnlyCollection<SAMVAD.DMS.Application.DTOs.District.DistrictPublicOptionDto>>>("api/districts/public");
        var districts = response?.Data ?? Array.Empty<SAMVAD.DMS.Application.DTOs.District.DistrictPublicOptionDto>();
        ViewBag.PublicDistrictOptions = districts;

        Guid? selectedDistrictId = null;
        if (!string.IsNullOrWhiteSpace(districtCode))
        {
            selectedDistrictId = districts.FirstOrDefault(x => string.Equals(x.Code, districtCode, StringComparison.OrdinalIgnoreCase))?.Id;
        }

        return View(new IncidentPublicSubmitDto
        {
            DistrictId = selectedDistrictId
        });
    }

    [HttpPost("report")]
    [HttpPost("report/{districtCode}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(IncidentPublicSubmitDto model, List<IFormFile>? mediaFiles, string? districtCode = null)
    {
        var districtOptionsResponse = await _httpService.GetAsync<ApiResponseDto<IReadOnlyCollection<SAMVAD.DMS.Application.DTOs.District.DistrictPublicOptionDto>>>("api/districts/public");
        var districtOptions = districtOptionsResponse?.Data ?? Array.Empty<SAMVAD.DMS.Application.DTOs.District.DistrictPublicOptionDto>();

        if ((!model.DistrictId.HasValue || model.DistrictId.Value == Guid.Empty) && !string.IsNullOrWhiteSpace(districtCode))
        {
            model.DistrictId = districtOptions.FirstOrDefault(x => string.Equals(x.Code, districtCode, StringComparison.OrdinalIgnoreCase))?.Id;
        }

        if (!ModelState.IsValid)
        {
            ViewBag.PublicDistrictOptions = districtOptions;
            return View(model);
        }

        var fields = new Dictionary<string, string?>
        {
            [nameof(IncidentPublicSubmitDto.DistrictId)] = model.DistrictId?.ToString(),
            [nameof(IncidentPublicSubmitDto.DisasterType)] = ((int)model.DisasterType).ToString(),
            [nameof(IncidentPublicSubmitDto.Priority)] = ((int)model.Priority).ToString(),
            [nameof(IncidentPublicSubmitDto.LocationGpsLat)] = model.LocationGpsLat?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [nameof(IncidentPublicSubmitDto.LocationGpsLng)] = model.LocationGpsLng?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [nameof(IncidentPublicSubmitDto.LocationText)] = model.LocationText,
            [nameof(IncidentPublicSubmitDto.Details)] = model.Details,
            [nameof(IncidentPublicSubmitDto.MobileNumber)] = model.MobileNumber
        };

        var response = await _httpService.PostMultipartAsync<ApiResponseDto<IncidentTrackingDto>>(
            "api/incidents/public",
            fields,
            mediaFiles,
            "mediaFiles");
        if (response?.Success == true)
        {
            return RedirectToAction(nameof(Success), new
            {
                incidentId = response.Data?.IncidentId,
                trackingToken = response.Data?.TrackingToken
            });
        }

        ModelState.AddModelError(string.Empty, response?.Message ?? "Unable to submit report.");
        ViewBag.PublicDistrictOptions = districtOptions;
        return View(model);
    }

    [HttpGet("report/success")]
    public IActionResult Success(string? incidentId = null, string? trackingToken = null)
    {
        ViewData["IncidentId"] = incidentId;
        ViewData["TrackingToken"] = trackingToken;
        return View();
    }

    [HttpGet("track/{trackingToken}")]
    public async Task<IActionResult> Track(string trackingToken)
    {
        var response = await _httpService.GetAsync<ApiResponseDto<IncidentTrackingDto>>($"api/incidents/track/{trackingToken}");
        return View(response?.Data);
    }
}