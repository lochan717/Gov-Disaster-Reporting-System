using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.DTOs.User;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminAccess")]
public class SettingsController : ControllerBase
{
    [HttpPost("update-profile")]
    public ActionResult<ApiResponseDto<bool>> UpdateProfile([FromBody] UpdateProfileDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Error("Invalid request data."));
        }

        return Ok(ApiResponseDto<bool>.Ok(true, "Profile updated."));
    }

    [HttpPost("change-password")]
    public ActionResult<ApiResponseDto<bool>> ChangePassword([FromBody] ChangePasswordDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Error("Invalid request data."));
        }

        return Ok(ApiResponseDto<bool>.Ok(true, "Password changed."));
    }
}