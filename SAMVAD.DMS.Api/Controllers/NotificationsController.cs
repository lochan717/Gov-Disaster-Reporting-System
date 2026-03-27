using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.Services;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdminOnly")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("incident-submitted/{incidentId:guid}")]
    public async Task<ActionResult<ApiResponseDto<bool>>> IncidentSubmitted(Guid incidentId)
    {
        await _notificationService.NotifyIncidentSubmittedAsync(incidentId);
        return Ok(ApiResponseDto<bool>.Ok(true));
    }

    [HttpPost("status-changed/{incidentId:guid}")]
    public async Task<ActionResult<ApiResponseDto<bool>>> StatusChanged(Guid incidentId)
    {
        await _notificationService.NotifyStatusChangedAsync(incidentId);
        return Ok(ApiResponseDto<bool>.Ok(true));
    }
}