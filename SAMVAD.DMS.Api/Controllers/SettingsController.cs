using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SAMVAD.DMS.Domain.Entities;
using SAMVAD.DMS.Application.DTOs.User;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminAccess")]
public class SettingsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public SettingsController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponseDto<UpdateProfileDto>>> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponseDto<UpdateProfileDto>.Error("Unauthorized access."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(ApiResponseDto<UpdateProfileDto>.Error("User not found."));
        }

        var dto = new UpdateProfileDto
        {
            FullName = user.FullName ?? string.Empty,
            ContactNumber = user.PhoneNumber
        };

        return Ok(ApiResponseDto<UpdateProfileDto>.Ok(dto));
    }

    [HttpPost("update-profile")]
    public async Task<ActionResult<ApiResponseDto<bool>>> UpdateProfile([FromBody] UpdateProfileDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Error("Invalid request data."));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Error("Unauthorized access."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(ApiResponseDto<bool>.Error("User not found."));
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.ContactNumber) ? null : request.ContactNumber.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(" ", updateResult.Errors.Select(x => x.Description));
            return BadRequest(ApiResponseDto<bool>.Error(string.IsNullOrWhiteSpace(errors) ? "Failed to update profile." : errors));
        }

        return Ok(ApiResponseDto<bool>.Ok(true, "Profile updated."));
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponseDto<bool>>> ChangePassword([FromBody] ChangePasswordDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Error("Invalid request data."));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponseDto<bool>.Error("Unauthorized access."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(ApiResponseDto<bool>.Error("User not found."));
        }

        var passwordResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!passwordResult.Succeeded)
        {
            var errors = string.Join(" ", passwordResult.Errors.Select(x => x.Description));
            return BadRequest(ApiResponseDto<bool>.Error(string.IsNullOrWhiteSpace(errors) ? "Failed to change password." : errors));
        }

        return Ok(ApiResponseDto<bool>.Ok(true, "Password changed."));
    }
}