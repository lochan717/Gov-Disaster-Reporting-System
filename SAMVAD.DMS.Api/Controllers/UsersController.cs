using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.DTOs.User;
using SAMVAD.DMS.Application.Services;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("form-options")]
    public async Task<ActionResult<ApiResponseDto<AdminFormOptionsDto>>> FormOptions()
    {
        var result = await _userService.GetAdminFormOptionsAsync();
        return result.IsSuccess
            ? Ok(ApiResponseDto<AdminFormOptionsDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<AdminFormOptionsDto>.Error(result.Message ?? "Failed."));
    }

    [HttpPost("list")]
    public async Task<ActionResult<ApiResponseDto<PaginatedResult<AdminListItemDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] string? role = null, [FromQuery] Guid? districtId = null)
    {
        var result = await _userService.GetAdminsAsync(page, pageSize, search, isActive, role, districtId);
        return result.IsSuccess
            ? Ok(ApiResponseDto<PaginatedResult<AdminListItemDto>>.Ok(result.Data))
            : BadRequest(ApiResponseDto<PaginatedResult<AdminListItemDto>>.Error(result.Message ?? "Failed."));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponseDto<AdminDetailDto>>> GetById(string id)
    {
        var result = await _userService.GetAdminByIdAsync(id);
        return result.IsSuccess
            ? Ok(ApiResponseDto<AdminDetailDto>.Ok(result.Data))
            : NotFound(ApiResponseDto<AdminDetailDto>.Error(result.Message ?? "Not found."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<AdminDetailDto>>> Create([FromBody] CreateAdminDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<AdminDetailDto>.Error("Invalid request data."));
        }

        var result = await _userService.CreateAdminAsync(request, User.Identity?.Name ?? "system");
        return result.IsSuccess
            ? Ok(ApiResponseDto<AdminDetailDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<AdminDetailDto>.Error(result.Message ?? "Failed."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponseDto<AdminDetailDto>>> Update(string id, [FromBody] UpdateAdminDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<AdminDetailDto>.Error("Invalid request data."));
        }

        var result = await _userService.UpdateAdminAsync(id, request, User.Identity?.Name ?? "system");
        return result.IsSuccess
            ? Ok(ApiResponseDto<AdminDetailDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<AdminDetailDto>.Error(result.Message ?? "Failed."));
    }

    [HttpPatch("{id}/toggle-status")]
    public async Task<ActionResult<ApiResponseDto<bool>>> ToggleStatus(string id)
    {
        var result = await _userService.ToggleAdminStatusAsync(id, User.Identity?.Name ?? "system");
        return result.IsSuccess
            ? Ok(ApiResponseDto<bool>.Ok(true, result.Message))
            : BadRequest(ApiResponseDto<bool>.Error(result.Message ?? "Failed."));
    }

    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult<ApiResponseDto<bool>>> ResetPassword(string id, [FromBody] ResetUserPasswordDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Error("Invalid request data."));
        }

        var result = await _userService.TriggerResetPasswordAsync(id, request, User.Identity?.Name ?? "system");
        return result.IsSuccess
            ? Ok(ApiResponseDto<bool>.Ok(true, result.Message))
            : BadRequest(ApiResponseDto<bool>.Error(result.Message ?? "Failed."));
    }
}