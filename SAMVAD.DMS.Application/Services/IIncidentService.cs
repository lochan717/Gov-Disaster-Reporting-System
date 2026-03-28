using SAMVAD.DMS.Application.DTOs.Incident;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Application.Services;

public interface IIncidentService
{
    Task<Result<IncidentTrackingDto>> CreatePublicIncidentAsync(
        IncidentPublicSubmitDto request,
        IReadOnlyCollection<IncidentUploadFileDto>? mediaFiles = null,
        CancellationToken cancellationToken = default);

    Task<Result<IncidentTrackingDto>> CreateAdminIncidentOnBehalfAsync(
        IncidentAdminSubmitDto request,
        string userId,
        string userName,
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<IncidentSubmissionOptionsDto>> GetSubmissionOptionsAsync(
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<PaginatedResult<IncidentListItemDto>>> GetIncidentsAsync(
        IncidentFilterDto filter,
        string userId,
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<IncidentDetailDto>> GetIncidentDetailAsync(
        Guid incidentId,
        string userId,
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UpdateStatusAsync(
        Guid incidentId,
        UpdateStatusDto request,
        string userId,
        string userName,
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<IncidentCommentDto>> AddCommentAsync(
        Guid incidentId,
        AddCommentDto request,
        string userId,
        string userName,
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<IncidentMediaDto>> UploadMediaAsync(
        Guid incidentId,
        string fileName,
        string contentType,
        Stream fileStream,
        string userId,
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<IncidentMediaFileDto>> GetIncidentMediaFileAsync(
        Guid incidentId,
        Guid mediaId,
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<IncidentMediaFileDto>> GetTrackingMediaFileAsync(
        string trackingToken,
        Guid mediaId,
        CancellationToken cancellationToken = default);

    Task<Result<byte[]>> ExportPdfAsync(
        Guid incidentId,
        string userId,
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<byte[]>> ExportExcelAsync(
        IncidentFilterDto filter,
        string userId,
        bool isSuperAdmin,
        IReadOnlyCollection<Guid> assignedDistrictIds,
        CancellationToken cancellationToken = default);

    Task<Result<IncidentTrackingDto>> GetTrackingDetailsAsync(
        string trackingToken,
        CancellationToken cancellationToken = default);
}