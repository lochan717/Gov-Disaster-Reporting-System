using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.DTOs.Incident;
using SAMVAD.DMS.Application.Services;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [AllowAnonymous]
    [HttpPost("public")]
    public async Task<ActionResult<ApiResponseDto<IncidentTrackingDto>>> PublicSubmit([FromForm] IncidentPublicSubmitDto request, [FromForm] List<IFormFile>? mediaFiles)
    {
        if (!ModelState.IsValid)
        {
            var validationMessage = string.Join(" ",
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct());

            return BadRequest(ApiResponseDto<IncidentTrackingDto>.Error(
                string.IsNullOrWhiteSpace(validationMessage) ? "Invalid request data." : validationMessage));
        }

        var media = mediaFiles?
            .Where(x => x is not null && x.Length > 0)
            .Select(x => new IncidentUploadFileDto
            {
                FileName = x.FileName,
                ContentType = x.ContentType
            })
            .ToArray();

        var result = await _incidentService.CreatePublicIncidentAsync(request, media);
        return result.IsSuccess
            ? Ok(ApiResponseDto<IncidentTrackingDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<IncidentTrackingDto>.Error(result.Message ?? "Failed."));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("submission-options")]
    public async Task<ActionResult<ApiResponseDto<IncidentSubmissionOptionsDto>>> SubmissionOptions()
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var assignedDistrictIds = GetAssignedDistrictIds();
        var result = await _incidentService.GetSubmissionOptionsAsync(isSuperAdmin, assignedDistrictIds);
        return result.IsSuccess
            ? Ok(ApiResponseDto<IncidentSubmissionOptionsDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<IncidentSubmissionOptionsDto>.Error(result.Message ?? "Failed."));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("on-behalf")]
    public async Task<ActionResult<ApiResponseDto<IncidentTrackingDto>>> SubmitOnBehalf([FromBody] IncidentAdminSubmitDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<IncidentTrackingDto>.Error("Invalid request data."));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userName = User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "Unknown";
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var assignedDistrictIds = GetAssignedDistrictIds();

        var result = await _incidentService.CreateAdminIncidentOnBehalfAsync(request, userId, userName, isSuperAdmin, assignedDistrictIds);
        return result.IsSuccess
            ? Ok(ApiResponseDto<IncidentTrackingDto>.Ok(result.Data, result.Message))
            : BadRequest(ApiResponseDto<IncidentTrackingDto>.Error(result.Message ?? "Failed."));
    }

    [AllowAnonymous]
    [HttpGet("track/{trackingToken}")]
    public async Task<ActionResult<ApiResponseDto<IncidentTrackingDto>>> Track(string trackingToken)
    {
        var result = await _incidentService.GetTrackingDetailsAsync(trackingToken);
        return result.IsSuccess
            ? Ok(ApiResponseDto<IncidentTrackingDto>.Ok(result.Data))
            : NotFound(ApiResponseDto<IncidentTrackingDto>.Error(result.Message ?? "Not found."));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("list")]
    public async Task<ActionResult<ApiResponseDto<PaginatedResult<IncidentListItemDto>>>> List([FromBody] IncidentFilterDto filter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var assignedDistrictIds = GetAssignedDistrictIds();
        var result = await _incidentService.GetIncidentsAsync(filter, userId, isSuperAdmin, assignedDistrictIds);
        return result.IsSuccess
            ? Ok(ApiResponseDto<PaginatedResult<IncidentListItemDto>>.Ok(result.Data))
            : BadRequest(ApiResponseDto<PaginatedResult<IncidentListItemDto>>.Error(result.Message ?? "Failed."));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponseDto<IncidentDetailDto>>> Get(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var assignedDistrictIds = GetAssignedDistrictIds();
        var result = await _incidentService.GetIncidentDetailAsync(id, userId, isSuperAdmin, assignedDistrictIds);
        return result.IsSuccess
            ? Ok(ApiResponseDto<IncidentDetailDto>.Ok(result.Data))
            : NotFound(ApiResponseDto<IncidentDetailDto>.Error(result.Message ?? "Not found."));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponseDto<bool>>> UpdateStatus(Guid id, [FromBody] UpdateStatusDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Error("Invalid request data."));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userName = User.Identity?.Name ?? "Unknown";
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var assignedDistrictIds = GetAssignedDistrictIds();
        var result = await _incidentService.UpdateStatusAsync(id, request, userId, userName, isSuperAdmin, assignedDistrictIds);
        return result.IsSuccess
            ? Ok(ApiResponseDto<bool>.Ok(true, result.Message))
            : BadRequest(ApiResponseDto<bool>.Error(result.Message ?? "Failed."));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponseDto<IncidentCommentDto>>> AddComment(Guid id, [FromBody] AddCommentDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<IncidentCommentDto>.Error("Invalid request data."));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userName = User.Identity?.Name ?? "Unknown";
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var assignedDistrictIds = GetAssignedDistrictIds();
        var result = await _incidentService.AddCommentAsync(id, request, userId, userName, isSuperAdmin, assignedDistrictIds);
        return result.IsSuccess
            ? Ok(ApiResponseDto<IncidentCommentDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<IncidentCommentDto>.Error(result.Message ?? "Failed."));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("{id:guid}/media")]
    public async Task<ActionResult<ApiResponseDto<IncidentMediaDto>>> UploadMedia(Guid id, IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponseDto<IncidentMediaDto>.Error("File is required."));
        }

        await using var stream = file.OpenReadStream();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var assignedDistrictIds = GetAssignedDistrictIds();
        var result = await _incidentService.UploadMediaAsync(id, file.FileName, file.ContentType, stream, userId, isSuperAdmin, assignedDistrictIds);
        return result.IsSuccess
            ? Ok(ApiResponseDto<IncidentMediaDto>.Ok(result.Data))
            : BadRequest(ApiResponseDto<IncidentMediaDto>.Error(result.Message ?? "Failed."));
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpGet("{id:guid}/export-pdf")]
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var assignedDistrictIds = GetAssignedDistrictIds();
        var result = await _incidentService.ExportPdfAsync(id, userId, isSuperAdmin, assignedDistrictIds);
        if (!result.IsSuccess || result.Data is null)
        {
            return BadRequest(ApiResponseDto<object>.Error(result.Message ?? "Failed."));
        }

        var detailResult = await _incidentService.GetIncidentDetailAsync(id, userId, isSuperAdmin, assignedDistrictIds);
        var incidentRef = detailResult.IsSuccess && detailResult.Data is not null
            ? detailResult.Data.IncidentId
            : id.ToString("N");

        var safeIncidentRef = incidentRef.Replace(" ", "_");
        var fileName = $"Incident_Report_{safeIncidentRef}_{DateTime.UtcNow:yyyyMMdd}.pdf";

        return File(result.Data, "application/pdf", fileName);
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("export-csv")]
    public async Task<IActionResult> ExportCsv([FromBody] IncidentFilterDto filter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var assignedDistrictIds = GetAssignedDistrictIds();
        var result = await _incidentService.ExportExcelAsync(filter, userId, isSuperAdmin, assignedDistrictIds);
        if (!result.IsSuccess || result.Data is null)
        {
            return BadRequest(ApiResponseDto<object>.Error(result.Message ?? "Failed."));
        }

        var districtSegment = filter.DistrictId.HasValue ? filter.DistrictId.Value.ToString("N") : "ALL";
        var fileName = $"DMS_Export_{districtSegment}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(result.Data, "text/csv", fileName);
    }

    [Authorize(Policy = "AdminAccess")]
    [HttpPost("export-excel")]
    public async Task<IActionResult> ExportExcel([FromBody] IncidentFilterDto filter)
    {
        return await ExportCsv(filter);
    }

    private IReadOnlyCollection<Guid> GetAssignedDistrictIds()
    {
        var raw = User.FindFirstValue("AssignedDistrictIds");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<Guid>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();
    }
}