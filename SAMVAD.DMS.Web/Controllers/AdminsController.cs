using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.DTOs.User;
using SAMVAD.DMS.Shared.Models;
using SAMVAD.DMS.Web.Services;

namespace SAMVAD.DMS.Web.Controllers;

[Authorize(Policy = "SuperAdminOnly")]
public class AdminsController : Controller
{
    private readonly IHttpService _httpService;

    public AdminsController(IHttpService httpService)
    {
        _httpService = httpService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await LoadFormOptionsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null, string? role = null, Guid? districtId = null)
    {
        var encodedSearch = Uri.EscapeDataString(search ?? string.Empty);
        var encodedRole = Uri.EscapeDataString(role ?? string.Empty);
        var isActiveValue = isActive.HasValue ? isActive.Value.ToString().ToLowerInvariant() : string.Empty;
        var districtValue = districtId.HasValue ? districtId.Value.ToString() : string.Empty;
        var response = await _httpService.PostAsync<ApiResponseDto<PaginatedResult<AdminListItemDto>>>($"api/users/list?page={page}&pageSize={pageSize}&search={encodedSearch}&isActive={isActiveValue}&role={encodedRole}&districtId={districtValue}", new { });
        var payload = response?.Data ?? PaginatedResult<AdminListItemDto>.Create(Array.Empty<AdminListItemDto>(), 0, page, pageSize);
        return PartialView("_List", payload);
    }

    [HttpGet]
    public async Task<IActionResult> Add()
    {
        await LoadFormOptionsAsync();
        return PartialView("_Add");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromBody] CreateAdminDto model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid admin data." : firstError });
        }

        var response = await _httpService.PostAsync<ApiResponseDto<AdminDetailDto>>("api/users", model);
        return Json(new
        {
            success = response?.Success == true,
            message = response?.Message ?? (response?.Success == true ? "Admin created successfully." : "Failed to create admin.")
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        await LoadFormOptionsAsync();
        var response = await _httpService.GetAsync<ApiResponseDto<AdminDetailDto>>($"api/users/{id}");
        var data = response?.Data;
        ViewBag.AdminId = data?.Id ?? id;
        ViewBag.SelectedDistrictIds = data?.AssignedDistrictIds ?? Array.Empty<Guid>();

        var model = new UpdateAdminDto
        {
            Name = data?.Name ?? string.Empty,
            RoleLabel = data?.RoleLabel ?? string.Empty,
            AssignedDistrictIds = (data?.AssignedDistrictIds ?? Array.Empty<Guid>()).ToList()
        };

        return PartialView("_Edit", model);
    }

    [HttpGet]
    public async Task<IActionResult> ResetPasswordForm(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invalid user id.");
        }

        var response = await _httpService.GetAsync<ApiResponseDto<AdminDetailDto>>($"api/users/{id}");
        if (response?.Success != true || response.Data is null)
        {
            return BadRequest("User not found.");
        }

        ViewBag.AdminId = response.Data.Id;
        ViewBag.AdminName = response.Data.Name;
        ViewBag.AdminEmail = response.Data.Email;
        return PartialView("_ResetPassword", new ResetUserPasswordDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [FromBody] UpdateAdminDto model)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Json(new { success = false, message = "Invalid admin id." });
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid admin data." : firstError });
        }

        var response = await _httpService.PutAsync<ApiResponseDto<AdminDetailDto>>($"api/users/{id}", model);
        return Json(new
        {
            success = response?.Success == true,
            message = response?.Message ?? (response?.Success == true ? "Admin updated successfully." : "Failed to update admin.")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Json(new { success = false, message = "Invalid admin id." });
        }

        var response = await _httpService.PatchAsync<ApiResponseDto<bool>>($"api/users/{id}/toggle-status", new { });
        return Json(new
        {
            success = response?.Success == true,
            message = response?.Message ?? (response?.Success == true ? "Admin status updated." : "Failed to update admin status.")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetUserPasswordDto model)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Json(new { success = false, message = "Invalid admin id." });
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid password data." : firstError });
        }

        var response = await _httpService.PostAsync<ApiResponseDto<bool>>($"api/users/{id}/reset-password", model);
        return Json(new
        {
            success = response?.Success == true,
            message = response?.Message ?? (response?.Success == true ? "Password reset successfully." : "Failed to reset password.")
        });
    }

    private async Task LoadFormOptionsAsync()
    {
        var response = await _httpService.GetAsync<ApiResponseDto<AdminFormOptionsDto>>("api/users/form-options");
        ViewBag.RoleOptions = response?.Data?.RoleOptions ?? Array.Empty<string>();
        ViewBag.DistrictOptions = response?.Data?.DistrictOptions ?? Array.Empty<AdminDistrictOptionDto>();
    }
}