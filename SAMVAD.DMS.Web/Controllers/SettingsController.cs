using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SAMVAD.DMS.Application.DTOs.User;
using SAMVAD.DMS.Shared.Models;
using SAMVAD.DMS.Web.Services;

namespace SAMVAD.DMS.Web.Controllers;

[Authorize(Policy = "AdminAccess")]
public class SettingsController : Controller
{
    private readonly IHttpService _httpService;

    public SettingsController(IHttpService httpService)
    {
        _httpService = httpService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profileResponse = await _httpService.GetAsync<ApiResponseDto<UpdateProfileDto>>("api/settings/profile");
        var profile = profileResponse?.Data;
        ViewBag.CurrentFullName = profile?.FullName ?? User.FindFirstValue("FullName") ?? User.Identity?.Name ?? string.Empty;
        ViewBag.CurrentContactNumber = profile?.ContactNumber;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid profile data." : firstError });
        }

        var response = await _httpService.PostAsync<ApiResponseDto<bool>>("api/settings/update-profile", model);
        return Json(new { success = response?.Success == true, message = response?.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault();
            return Json(new { success = false, message = string.IsNullOrWhiteSpace(firstError) ? "Invalid password data." : firstError });
        }

        var response = await _httpService.PostAsync<ApiResponseDto<bool>>("api/settings/change-password", model);
        return Json(new { success = response?.Success == true, message = response?.Message });
    }
}