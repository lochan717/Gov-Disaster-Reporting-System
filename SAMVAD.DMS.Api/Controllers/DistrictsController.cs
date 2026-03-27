using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.DTOs.District;
using SAMVAD.DMS.Application.Services;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdminOnly")]
public class DistrictsController : ControllerBase
{
    private readonly IDistrictService _districtService;

    public DistrictsController(IDistrictService districtService)
    {
        _districtService = districtService;
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<ApiResponseDto<IReadOnlyCollection<DistrictPublicOptionDto>>>> Public()
    {
        var result = await _districtService.GetPublicActiveDistrictsAsync();
        return result.IsSuccess
            ? Ok(ApiResponseDto<IReadOnlyCollection<DistrictPublicOptionDto>>.Ok(result.Data))
            : BadRequest(ApiResponseDto<IReadOnlyCollection<DistrictPublicOptionDto>>.Error(result.Message ?? "Failed."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponseDto<PaginatedResult<DistrictListItemDto>>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var result = await _districtService.GetDistrictsAsync(page, pageSize, search);
        return result.IsSuccess
            ? Ok(ApiResponseDto<PaginatedResult<DistrictListItemDto>>.Ok(result.Data))
            : BadRequest(ApiResponseDto<PaginatedResult<DistrictListItemDto>>.Error(result.Message ?? "Failed."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<DistrictResponseDto>>> GetById(Guid id)
    {
        var result = await _districtService.GetDistrictByIdAsync(id);
        return result.IsSuccess
            ? Ok(ApiResponseDto<DistrictResponseDto>.Ok(result.Data))
            : NotFound(ApiResponseDto<DistrictResponseDto>.Error(result.Message ?? "Not found."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponseDto<DistrictResponseDto>>> Create([FromBody] DistrictRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<DistrictResponseDto>.Error("Invalid request data."));
        }

        var userId = User.Identity?.Name ?? "system";
        var result = await _districtService.CreateDistrictAsync(request, userId);
        return result.IsSuccess
            ? Ok(ApiResponseDto<DistrictResponseDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<DistrictResponseDto>.Error(result.Message ?? "Failed."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<DistrictResponseDto>>> Update(Guid id, [FromBody] DistrictRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<DistrictResponseDto>.Error("Invalid request data."));
        }

        var userId = User.Identity?.Name ?? "system";
        var result = await _districtService.UpdateDistrictAsync(id, request, userId);
        return result.IsSuccess
            ? Ok(ApiResponseDto<DistrictResponseDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<DistrictResponseDto>.Error(result.Message ?? "Failed."));
    }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<ActionResult<ApiResponseDto<bool>>> ToggleStatus(Guid id)
    {
        var userId = User.Identity?.Name ?? "system";
        var result = await _districtService.ToggleDistrictStatusAsync(id, userId);
        return result.IsSuccess
            ? Ok(ApiResponseDto<bool>.Ok(true, result.Message))
            : BadRequest(ApiResponseDto<bool>.Error(result.Message ?? "Failed."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<bool>>> Delete(Guid id)
    {
        var userId = User.Identity?.Name ?? "system";
        var result = await _districtService.DeleteDistrictAsync(id, userId);
        return result.IsSuccess
            ? Ok(ApiResponseDto<bool>.Ok(true, result.Message))
            : BadRequest(ApiResponseDto<bool>.Error(result.Message ?? "Failed."));
    }
}