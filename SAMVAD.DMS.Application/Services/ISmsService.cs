namespace SAMVAD.DMS.Application.Services;

public interface ISmsService
{
    Task SendAsync(string mobileNumber, string message, CancellationToken cancellationToken = default);
}