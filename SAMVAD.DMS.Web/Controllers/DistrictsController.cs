using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.DTOs.District;
using SAMVAD.DMS.Shared.Models;
using SAMVAD.DMS.Web.Services;

namespace SAMVAD.DMS.Web.Controllers;

[Authorize(Policy = "SuperAdminOnly")]
public class DistrictsController : Controller
{
    private readonly IHttpService _httpService;

    public DistrictsController(IHttpService httpService)
    {
        _httpService = httpService;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, string? search = null)
    {
        var encodedSearch = Uri.EscapeDataString(search ?? string.Empty);
        var response = await _httpService.GetAsync<ApiResponseDto<PaginatedResult<DistrictListItemDto>>>($"api/districts?page={page}&pageSize={pageSize}&search={encodedSearch}");
        var payload = response?.Data ?? PaginatedResult<DistrictListItemDto>.Create(Array.Empty<DistrictListItemDto>(), 0, page, pageSize);
        return PartialView("_List", payload);
    }

    [HttpGet]
    public IActionResult Add() => PartialView("_Add");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(DistrictRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid district data." : firstError });
        }

        var response = await _httpService.PostAsync<ApiResponseDto<DistrictResponseDto>>("api/districts", model);
        return Json(new { success = response?.Success == true, message = response?.Message });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var response = await _httpService.GetAsync<ApiResponseDto<DistrictResponseDto>>($"api/districts/{id}");
        var data = response?.Data;
        ViewBag.DistrictId = data?.Id ?? id;
        var model = new DistrictRequestDto
        {
            Name = data?.Name ?? string.Empty,
            Code = data?.Code ?? string.Empty,
            State = data?.State ?? string.Empty,
            IsActive = data?.IsActive ?? true
        };

        return PartialView("_Edit", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [FromBody] DistrictRequestDto model)
    {
        if (id == Guid.Empty)
        {
            return Json(new { success = false, message = "Invalid district id." });
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid district data." : firstError });
        }

        var response = await _httpService.PutAsync<ApiResponseDto<DistrictResponseDto>>($"api/districts/{id}", model);
        return Json(new { success = response?.Success == true, message = response?.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var response = await _httpService.PatchAsync<ApiResponseDto<bool>>($"api/districts/{id}/toggle-status", new { });
        return Json(new { success = response?.Success == true, message = response?.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _httpService.DeleteAsync<ApiResponseDto<bool>>($"api/districts/{id}");
        return Json(new { success = response?.Success == true, message = response?.Message });
    }
}