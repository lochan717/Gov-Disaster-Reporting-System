using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SAMVAD.DMS.Application.DTOs.Auth;
using SAMVAD.DMS.Shared.Models;
using SAMVAD.DMS.Web.Services;

namespace SAMVAD.DMS.Web.Controllers;

public class AccountController : Controller
{
    private readonly IHttpService _httpService;

    public AccountController(IHttpService httpService)
    {
        _httpService = httpService;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestDto model)
    {
        if (!ModelState.IsValid) return View(model);
        var response = await _httpService.PostAsync<ApiResponseDto<LoginResponseDto>>("api/authentication/login", model);
        if (response?.Success != true || response.Data is null)
        {
            ModelState.AddModelError(string.Empty, response?.Message ?? "Invalid credentials.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, response.Data.Id),
            new(ClaimTypes.Name, response.Data.Username),
            new(ClaimTypes.Email, response.Data.Email),
            new("FullName", response.Data.FullName),
            new("RoleLabel", response.Data.RoleLabel)
        };

        foreach (var role in response.Data.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
                Items =
                {
                    ["access_token"] = response.Data.Token
                }
            });

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto model)
    {
        if (!ModelState.IsValid) return View(model);
        await _httpService.PostAsync<ApiResponseDto<bool>>("api/authentication/forgot-password", model);
        TempData["Success"] = "If the email is registered, a reset link has been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        return View(new ResetPasswordRequestDto
        {
            Token = token ?? string.Empty,
            Email = email ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto model)
    {
        if (!ModelState.IsValid) return View(model);
        var response = await _httpService.PostAsync<ApiResponseDto<bool>>("api/authentication/reset-password", model);
        if (response?.Success != true)
        {
            ModelState.AddModelError(string.Empty, response?.Message ?? "Reset failed.");
            return View(model);
        }

        TempData["Success"] = "Password updated. Please login.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}