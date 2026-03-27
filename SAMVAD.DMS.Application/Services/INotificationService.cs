namespace SAMVAD.DMS.Application.Services;

public interface INotificationService
{
    Task NotifyIncidentSubmittedAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task NotifyStatusChangedAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task NotifyEmergencyIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default);
}