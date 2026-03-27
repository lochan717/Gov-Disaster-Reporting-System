using SAMVAD.DMS.Domain.Entities;
using SAMVAD.DMS.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SAMVAD.DMS.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly string _publicBaseUrl;

    public NotificationService(IUnitOfWork unitOfWork, IEmailService emailService, ISmsService smsService, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _smsService = smsService;
        _publicBaseUrl = configuration["PublicBaseUrl"]?.TrimEnd('/') ?? "https://localhost:7002";
    }

    public async Task NotifyEmergencyIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = _unitOfWork.Query<Incident>().FirstOrDefault(x => x.Id == incidentId);
        if (incident is null)
        {
            return;
        }

        var district = _unitOfWork.Query<District>().FirstOrDefault(x => x.Id == incident.DistrictId);
        var message = $"Emergency alert: Incident {incident.IncidentId} in {district?.Name ?? "Unknown District"}. Please respond immediately.";

        var userMobiles = _unitOfWork.Query<ApplicationUserDistrict>()
            .Where(x => x.DistrictId == incident.DistrictId)
            .Select(x => x.ApplicationUser)
            .Where(x => x != null && x.IsActive && !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .Select(x => x!.PhoneNumber!)
            .Distinct()
            .ToArray();

        foreach (var mobile in userMobiles)
        {
            await _smsService.SendAsync(mobile, message, cancellationToken);
        }
    }

    public async Task NotifyIncidentSubmittedAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = _unitOfWork.Query<Incident>().FirstOrDefault(x => x.Id == incidentId);
        if (incident is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(incident.ReporterMobile))
        {
            var trackingLink = $"{_publicBaseUrl}/track/{incident.TrackingToken}";
            var message = $"Your report {incident.IncidentId} has been received. Track status here: {trackingLink}";
            await _smsService.SendAsync(incident.ReporterMobile, message, cancellationToken);
        }

        var district = _unitOfWork.Query<District>().FirstOrDefault(x => x.Id == incident.DistrictId);
        var adminEmails = _unitOfWork.Query<ApplicationUserDistrict>()
            .Where(x => x.DistrictId == incident.DistrictId)
            .Select(x => x.ApplicationUser)
            .Where(x => x != null && x.IsActive && !string.IsNullOrWhiteSpace(x.Email))
            .Select(x => x!.Email!)
            .Distinct()
            .ToArray();

        foreach (var email in adminEmails)
        {
            await _emailService.SendTemplatedAsync(
                email,
                $"New Incident Submitted: {incident.IncidentId}",
                "IncidentSubmittedAdmin",
                new Dictionary<string, string>
                {
                    ["AppName"] = "SAMVAD DMS",
                    ["IncidentCode"] = incident.IncidentId,
                    ["DistrictName"] = district?.Name ?? "Unknown District",
                    ["DisasterType"] = incident.DisasterType.ToString(),
                    ["Priority"] = incident.Priority.ToString()
                },
                cancellationToken);
        }
    }

    public async Task NotifyStatusChangedAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var incident = _unitOfWork.Query<Incident>().FirstOrDefault(x => x.Id == incidentId);
        if (incident is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(incident.ReporterMobile))
        {
            var message = $"Update for incident {incident.IncidentId}: current status is {incident.Status}.";
            await _smsService.SendAsync(incident.ReporterMobile, message, cancellationToken);
        }
    }
}