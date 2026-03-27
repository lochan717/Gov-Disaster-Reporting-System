using System.Text.Json;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SAMVAD.DMS.Application.DTOs.Incident;
using SAMVAD.DMS.Domain.Entities;
using SAMVAD.DMS.Domain.Enums;
using SAMVAD.DMS.Domain.Interfaces;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Application.Services;

public class IncidentService : IIncidentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public IncidentService(IUnitOfWork unitOfWork, IAuditService auditService, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<Result<IncidentCommentDto>> AddCommentAsync(Guid incidentId, AddCommentDto request, string userId, string userName, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds, CancellationToken cancellationToken = default)
    {
        var incident = _unitOfWork.Query<Incident>().FirstOrDefault(x => x.Id == incidentId);
        if (incident is null)
        {
            return Result<IncidentCommentDto>.Fail("Incident not found.");
        }

        if (!CanAccessIncident(incident.DistrictId, isSuperAdmin, assignedDistrictIds))
        {
            return Result<IncidentCommentDto>.Fail("Access denied.");
        }

        var comment = new IncidentComment
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            AuthorId = userId,
            AuthorName = userName,
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAuditAsync(
            userId,
            "AddComment",
            nameof(Incident),
            incidentId.ToString(),
            null,
            SerializeIncidentCommentAuditSnapshot(comment),
            cancellationToken);

        return Result<IncidentCommentDto>.Succeed(new IncidentCommentDto
        {
            Id = comment.Id,
            AuthorId = comment.AuthorId,
            AuthorName = comment.AuthorName,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt
        });
    }

    public async Task<Result<IncidentTrackingDto>> CreatePublicIncidentAsync(IncidentPublicSubmitDto request, IReadOnlyCollection<IncidentUploadFileDto>? mediaFiles = null, CancellationToken cancellationToken = default)
    {
        if (!request.DistrictId.HasValue || request.DistrictId.Value == Guid.Empty)
        {
            return Result<IncidentTrackingDto>.Fail("District is required.");
        }

        var district = _unitOfWork.Query<District>().FirstOrDefault(x => x.Id == request.DistrictId.Value && x.IsActive);
        if (district is null)
        {
            return Result<IncidentTrackingDto>.Fail("District not found.");
        }

        if (!HasLocation(request.LocationText, request.LocationGpsLat, request.LocationGpsLng))
        {
            return Result<IncidentTrackingDto>.Fail("Location text is required if GPS is unavailable.");
        }

        if (mediaFiles is not null && mediaFiles.Count > 5)
        {
            return Result<IncidentTrackingDto>.Fail("Maximum 5 files allowed per incident.");
        }

        return await CreateIncidentCoreAsync(
            district,
            request.DisasterType,
            request.Priority,
            request.LocationGpsLat,
            request.LocationGpsLng,
            request.LocationText,
            request.MobileNumber,
            request.Details,
            "SYSTEM",
            "System",
            MediaUploadedBy.Citizen,
            null,
            mediaFiles,
            cancellationToken);
    }

    public async Task<Result<IncidentTrackingDto>> CreateAdminIncidentOnBehalfAsync(IncidentAdminSubmitDto request, string userId, string userName, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds, CancellationToken cancellationToken = default)
    {
        var district = _unitOfWork.Query<District>().FirstOrDefault(x => x.Id == request.DistrictId && x.IsActive);
        if (district is null)
        {
            return Result<IncidentTrackingDto>.Fail("District not found.");
        }

        if (!isSuperAdmin && !assignedDistrictIds.Contains(request.DistrictId))
        {
            return Result<IncidentTrackingDto>.Fail("Access denied for selected district.");
        }

        if (!HasLocation(request.LocationText, request.LocationGpsLat, request.LocationGpsLng))
        {
            return Result<IncidentTrackingDto>.Fail("Location text is required if GPS is unavailable.");
        }

        var channel = string.IsNullOrWhiteSpace(request.SubmissionChannel) ? "Other" : request.SubmissionChannel.Trim();
        return await CreateIncidentCoreAsync(
            district,
            request.DisasterType,
            request.Priority,
            request.LocationGpsLat,
            request.LocationGpsLng,
            request.LocationText,
            request.MobileNumber,
            request.Details,
            userId,
            userName,
            MediaUploadedBy.Admin,
            channel,
            null,
            cancellationToken);
    }

    public Task<Result<IncidentSubmissionOptionsDto>> GetSubmissionOptionsAsync(bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds, CancellationToken cancellationToken = default)
    {
        var districts = _unitOfWork.Query<District>()
            .Where(x => x.IsActive)
            .Where(x => isSuperAdmin || assignedDistrictIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new IncidentSubmissionDistrictOptionDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code
            })
            .ToArray();

        var options = new IncidentSubmissionOptionsDto
        {
            Districts = districts,
            Channels = new[]
            {
                "Phone",
                "Walk-in",
                "WhatsApp",
                "Helpline",
                "Other"
            }
        };

        return Task.FromResult(Result<IncidentSubmissionOptionsDto>.Succeed(options));
    }

    private async Task<Result<IncidentTrackingDto>> CreateIncidentCoreAsync(
        District district,
        DisasterType disasterType,
        IncidentPriority priority,
        decimal? locationGpsLat,
        decimal? locationGpsLng,
        string? locationText,
        string mobileNumber,
        string? details,
        string actorId,
        string actorName,
        MediaUploadedBy mediaUploadedBy,
        string? submissionChannel,
        IReadOnlyCollection<IncidentUploadFileDto>? mediaFiles,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var dayStart = utcNow.Date;
        var dayEnd = dayStart.AddDays(1);
        var todayCount = _unitOfWork.Query<Incident>()
            .Count(x => x.DistrictId == district.Id && x.CreatedAt >= dayStart && x.CreatedAt < dayEnd);

        var nextCounter = todayCount + 1;
        var incidentCode = $"{district.Code}-{utcNow:yyyyMMdd}-{nextCounter:0000}";

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentCode,
            DistrictId = district.Id,
            DisasterType = disasterType,
            Priority = priority,
            LocationGpsLat = locationGpsLat,
            LocationGpsLng = locationGpsLng,
            LocationText = locationText?.Trim() ?? string.Empty,
            ReporterMobile = mobileNumber,
            Status = IncidentStatus.Open,
            TrackingToken = Guid.NewGuid().ToString("N"),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        await _unitOfWork.AddAsync(incident, cancellationToken);

        var statusNote = mediaUploadedBy == MediaUploadedBy.Admin && !string.IsNullOrWhiteSpace(submissionChannel)
            ? $"Incident submitted by admin via {submissionChannel}."
            : "Incident submitted";

        var statusHistory = new IncidentStatusHistory
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            FromStatus = IncidentStatus.Open,
            ToStatus = IncidentStatus.Open,
            ChangedById = actorId,
            ChangedByName = actorName,
            Note = statusNote,
            Timestamp = utcNow
        };

        await _unitOfWork.AddAsync(statusHistory, cancellationToken);

        if (!string.IsNullOrWhiteSpace(details))
        {
            await _unitOfWork.AddAsync(new IncidentComment
            {
                Id = Guid.NewGuid(),
                IncidentId = incident.Id,
                AuthorId = actorId,
                AuthorName = actorName,
                Body = details.Trim(),
                CreatedAt = utcNow
            }, cancellationToken);
        }

        if (mediaFiles is not null)
        {
            foreach (var mediaFile in mediaFiles.Take(5))
            {
                var fileName = string.IsNullOrWhiteSpace(mediaFile.FileName)
                    ? "attachment"
                    : Path.GetFileName(mediaFile.FileName);
                var mediaType = mediaFile.ContentType.Contains("video", StringComparison.OrdinalIgnoreCase)
                    ? IncidentMediaType.Video
                    : IncidentMediaType.Photo;

                await _unitOfWork.AddAsync(new IncidentMedia
                {
                    Id = Guid.NewGuid(),
                    IncidentId = incident.Id,
                    FileName = fileName,
                    FilePath = $"uploads/incidents/{Guid.NewGuid():N}_{fileName}",
                    MediaType = mediaType,
                    UploadedBy = mediaUploadedBy,
                    UploadedAt = utcNow
                }, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAuditAsync(
            actorId,
            "Create",
            nameof(Incident),
            incident.Id.ToString(),
            null,
            SerializeIncidentAuditSnapshot(incident),
            cancellationToken);

        await _notificationService.NotifyIncidentSubmittedAsync(incident.Id, cancellationToken);
        if (incident.Priority == IncidentPriority.Emergency)
        {
            await _notificationService.NotifyEmergencyIncidentAsync(incident.Id, cancellationToken);
        }

        return Result<IncidentTrackingDto>.Succeed(new IncidentTrackingDto
        {
            IncidentId = incident.IncidentId,
            TrackingToken = incident.TrackingToken,
            DisasterType = incident.DisasterType,
            Status = incident.Status,
            LastUpdatedAt = incident.UpdatedAt
        });
    }

    public Task<Result<byte[]>> ExportExcelAsync(IncidentFilterDto filter, string userId, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds, CancellationToken cancellationToken = default)
    {
        var incidents = ApplyIncidentSorting(BuildFilteredIncidentQuery(filter, isSuperAdmin, assignedDistrictIds), filter)
            .ToList();

        var incidentIds = incidents.Select(x => x.Id).ToArray();
        var districtLookup = _unitOfWork.Query<District>().ToDictionary(x => x.Id, x => x.Name);
        var mediaCountLookup = _unitOfWork.Query<IncidentMedia>()
            .Where(x => incidentIds.Contains(x.IncidentId))
            .GroupBy(x => x.IncidentId)
            .ToDictionary(x => x.Key, x => x.Count());

        var commentCountLookup = _unitOfWork.Query<IncidentComment>()
            .Where(x => incidentIds.Contains(x.IncidentId))
            .GroupBy(x => x.IncidentId)
            .ToDictionary(x => x.Key, x => x.Count());

        var sb = new StringBuilder();
        sb.AppendLine("IncidentID,District,DisasterType,Priority,Location,GpsLat,GpsLng,ReporterMobile,CreatedAtUtc,UpdatedAtUtc,ClosedAtUtc,Status,ResolutionNote,MediaCount,CommentCount");

        foreach (var incident in incidents)
        {
            var districtName = districtLookup.TryGetValue(incident.DistrictId, out var value) ? value : string.Empty;
            var mediaCount = mediaCountLookup.TryGetValue(incident.Id, out var media) ? media : 0;
            var commentCount = commentCountLookup.TryGetValue(incident.Id, out var comments) ? comments : 0;

            sb.AppendLine(string.Join(',',
                Csv(incident.IncidentId),
                Csv(districtName),
                Csv(incident.DisasterType.ToString()),
                Csv(incident.Priority.ToString()),
                Csv(incident.LocationText),
                Csv(incident.LocationGpsLat?.ToString() ?? string.Empty),
                Csv(incident.LocationGpsLng?.ToString() ?? string.Empty),
                Csv(incident.ReporterMobile),
                Csv(incident.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(incident.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(incident.ClosedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty),
                Csv(incident.Status.ToString()),
                Csv(incident.ResolutionNote ?? string.Empty),
                Csv(mediaCount.ToString()),
                Csv(commentCount.ToString())));
        }

        return Task.FromResult(Result<byte[]>.Succeed(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    public async Task<Result<byte[]>> ExportPdfAsync(Guid incidentId, string userId, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds, CancellationToken cancellationToken = default)
    {
        var detailResult = await GetIncidentDetailAsync(incidentId, userId, isSuperAdmin, assignedDistrictIds, cancellationToken);
        if (!detailResult.IsSuccess || detailResult.Data is null)
        {
            return Result<byte[]>.Fail(detailResult.Message ?? "Incident not found.");
        }

        var incident = detailResult.Data;
        var generatedBy = _unitOfWork.Query<ApplicationUser>().FirstOrDefault(x => x.Id == userId)?.FullName ?? "System";

        QuestPDF.Settings.License = LicenseType.Community;

        var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Spacing(4);
                        column.Item().Text($"INCIDENT REPORT - {incident.DistrictName}").SemiBold().FontSize(17).FontColor(Colors.Orange.Darken2);
                        column.Item().Text($"Incident ID: {incident.IncidentId}").FontSize(10);
                        column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").FontSize(9).FontColor(Colors.Grey.Darken1);
                        column.Item().Text($"Generated by: {generatedBy}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        column.Item().Text("District Approval Stamp: __________________").FontSize(9);
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(12);
                        column.Item().Element(c => ComposeSummary(c, incident));
                        column.Item().Element(c => ComposeVisualEvidence(c, incident));
                        column.Item().Element(c => ComposeOperationalNarrative(c, incident));
                    });

                    page.Footer().Column(column =>
                    {
                        column.Item().AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                        column.Item().AlignCenter().Text("Confidential - For Internal District Use Only").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            })
            .GeneratePdf();

        return Result<byte[]>.Succeed(bytes);
    }

    public Task<Result<IncidentDetailDto>> GetIncidentDetailAsync(Guid incidentId, string userId, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds, CancellationToken cancellationToken = default)
    {
        var incident = _unitOfWork.Query<Incident>().FirstOrDefault(x => x.Id == incidentId);
        if (incident is null)
        {
            return Task.FromResult(Result<IncidentDetailDto>.Fail("Incident not found."));
        }

        if (!CanAccessIncident(incident.DistrictId, isSuperAdmin, assignedDistrictIds))
        {
            return Task.FromResult(Result<IncidentDetailDto>.Fail("Access denied."));
        }

        var district = _unitOfWork.Query<District>().FirstOrDefault(x => x.Id == incident.DistrictId);
        var comments = _unitOfWork.Query<IncidentComment>()
            .Where(x => x.IncidentId == incidentId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new IncidentCommentDto
            {
                Id = x.Id,
                AuthorId = x.AuthorId,
                AuthorName = x.AuthorName,
                Body = x.Body,
                CreatedAt = x.CreatedAt
            })
            .ToArray();

        var statusHistory = _unitOfWork.Query<IncidentStatusHistory>()
            .Where(x => x.IncidentId == incidentId)
            .OrderBy(x => x.Timestamp)
            .Select(x => new IncidentStatusHistoryDto
            {
                FromStatus = x.FromStatus,
                ToStatus = x.ToStatus,
                ChangedById = x.ChangedById,
                ChangedByName = x.ChangedByName,
                Note = x.Note,
                Timestamp = x.Timestamp
            })
            .ToArray();

        var media = _unitOfWork.Query<IncidentMedia>()
            .Where(x => x.IncidentId == incidentId)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new IncidentMediaDto
            {
                Id = x.Id,
                FileName = x.FileName,
                FilePath = x.FilePath,
                MediaType = x.MediaType,
                UploadedBy = x.UploadedBy,
                UploadedAt = x.UploadedAt
            })
            .ToArray();

        var payload = new IncidentDetailDto
        {
            Id = incident.Id,
            IncidentId = incident.IncidentId,
            DistrictId = incident.DistrictId,
            DistrictName = district?.Name ?? string.Empty,
            DisasterType = incident.DisasterType,
            Priority = incident.Priority,
            LocationGpsLat = incident.LocationGpsLat,
            LocationGpsLng = incident.LocationGpsLng,
            LocationText = incident.LocationText,
            ReporterMobile = incident.ReporterMobile,
            Status = incident.Status,
            ResolutionNote = incident.ResolutionNote,
            CreatedAt = incident.CreatedAt,
            UpdatedAt = incident.UpdatedAt,
            ClosedAt = incident.ClosedAt,
            StatusHistory = statusHistory,
            Comments = comments,
            MediaFiles = media
        };

        return Task.FromResult(Result<IncidentDetailDto>.Succeed(payload));
    }

    public Task<Result<PaginatedResult<IncidentListItemDto>>> GetIncidentsAsync(IncidentFilterDto filter, string userId, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds, CancellationToken cancellationToken = default)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;
        var incidents = ApplyIncidentSorting(BuildFilteredIncidentQuery(filter, isSuperAdmin, assignedDistrictIds), filter);

        var districtLookup = _unitOfWork.Query<District>()
            .ToDictionary(x => x.Id, x => x.Name);

        var totalCount = incidents.Count();
        var items = incidents
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .Select(x => new IncidentListItemDto
            {
                Id = x.Id,
                IncidentId = x.IncidentId,
                DistrictId = x.DistrictId,
                DistrictName = districtLookup.TryGetValue(x.DistrictId, out var districtName) ? districtName : string.Empty,
                DisasterType = x.DisasterType,
                Priority = x.Priority,
                LocationText = x.LocationText,
                CreatedAt = x.CreatedAt,
                Status = x.Status
            })
            .ToArray();

        var payload = PaginatedResult<IncidentListItemDto>.Create(items, totalCount, page, pageSize);
        return Task.FromResult(Result<PaginatedResult<IncidentListItemDto>>.Succeed(payload));
    }

    private IQueryable<Incident> BuildFilteredIncidentQuery(IncidentFilterDto filter, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds)
    {
        var incidents = _unitOfWork.Query<Incident>().AsQueryable();

        if (!isSuperAdmin)
        {
            incidents = incidents.Where(x => assignedDistrictIds.Contains(x.DistrictId));
        }

        if (filter.DistrictId.HasValue)
        {
            incidents = incidents.Where(x => x.DistrictId == filter.DistrictId.Value);
        }

        if (filter.Status.HasValue)
        {
            incidents = incidents.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.DisasterType.HasValue)
        {
            incidents = incidents.Where(x => x.DisasterType == filter.DisasterType.Value);
        }

        if (filter.Priority.HasValue)
        {
            incidents = incidents.Where(x => x.Priority == filter.Priority.Value);
        }

        if (filter.FromDate.HasValue)
        {
            incidents = incidents.Where(x => x.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            incidents = incidents.Where(x => x.CreatedAt <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLowerInvariant();
            incidents = incidents.Where(x => x.IncidentId.ToLower().Contains(term) || x.LocationText.ToLower().Contains(term));
        }

        return incidents;
    }

    private static IQueryable<Incident> ApplyIncidentSorting(IQueryable<Incident> incidents, IncidentFilterDto filter)
    {
        var sortBy = (filter.SortBy ?? "createdat").Trim().ToLowerInvariant();
        var isDescending = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "incidentid" => isDescending ? incidents.OrderByDescending(x => x.IncidentId) : incidents.OrderBy(x => x.IncidentId),
            "status" => isDescending ? incidents.OrderByDescending(x => x.Status) : incidents.OrderBy(x => x.Status),
            "priority" => isDescending ? incidents.OrderByDescending(x => x.Priority) : incidents.OrderBy(x => x.Priority),
            "disastertype" => isDescending ? incidents.OrderByDescending(x => x.DisasterType) : incidents.OrderBy(x => x.DisasterType),
            _ => isDescending ? incidents.OrderByDescending(x => x.CreatedAt) : incidents.OrderBy(x => x.CreatedAt)
        };
    }

    public Task<Result<IncidentTrackingDto>> GetTrackingDetailsAsync(string trackingToken, CancellationToken cancellationToken = default)
    {
        var incident = _unitOfWork.Query<Incident>().FirstOrDefault(x => x.TrackingToken == trackingToken);
        if (incident is null)
        {
            return Task.FromResult(Result<IncidentTrackingDto>.Fail("Incident not found."));
        }

        return Task.FromResult(Result<IncidentTrackingDto>.Succeed(new IncidentTrackingDto
        {
            IncidentId = incident.IncidentId,
            TrackingToken = incident.TrackingToken,
            DisasterType = incident.DisasterType,
            Status = incident.Status,
            LastUpdatedAt = incident.UpdatedAt
        }));
    }

    public async Task<Result<IncidentMediaDto>> UploadMediaAsync(Guid incidentId, string fileName, string contentType, Stream fileStream, string userId, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds, CancellationToken cancellationToken = default)
    {
        var incident = _unitOfWork.Query<Incident>().FirstOrDefault(x => x.Id == incidentId);
        if (incident is null)
        {
            return Result<IncidentMediaDto>.Fail("Incident not found.");
        }

        if (!CanAccessIncident(incident.DistrictId, isSuperAdmin, assignedDistrictIds))
        {
            return Result<IncidentMediaDto>.Fail("Access denied.");
        }

        var existingCount = _unitOfWork.Query<IncidentMedia>().Count(x => x.IncidentId == incidentId);
        if (existingCount >= 5)
        {
            return Result<IncidentMediaDto>.Fail("Maximum 5 files allowed per incident.");
        }

        var mediaType = contentType.Contains("video", StringComparison.OrdinalIgnoreCase)
            ? IncidentMediaType.Video
            : IncidentMediaType.Photo;

        var media = new IncidentMedia
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            FileName = fileName,
            FilePath = $"uploads/incidents/{Guid.NewGuid():N}_{fileName}",
            MediaType = mediaType,
            UploadedBy = MediaUploadedBy.Admin,
            UploadedAt = DateTime.UtcNow
        };

        await _unitOfWork.AddAsync(media, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<IncidentMediaDto>.Succeed(new IncidentMediaDto
        {
            Id = media.Id,
            FileName = media.FileName,
            FilePath = media.FilePath,
            MediaType = media.MediaType,
            UploadedBy = media.UploadedBy,
            UploadedAt = media.UploadedAt
        });
    }

    public async Task<Result<bool>> UpdateStatusAsync(Guid incidentId, UpdateStatusDto request, string userId, string userName, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds, CancellationToken cancellationToken = default)
    {
        var incident = _unitOfWork.Query<Incident>().FirstOrDefault(x => x.Id == incidentId);
        if (incident is null)
        {
            return Result<bool>.Fail("Incident not found.");
        }

        if (!CanAccessIncident(incident.DistrictId, isSuperAdmin, assignedDistrictIds))
        {
            return Result<bool>.Fail("Access denied.");
        }

        if (!IsValidTransition(incident.Status, request.NewStatus, request.Note))
        {
            return Result<bool>.Fail("Invalid status transition.");
        }

        var oldValues = SerializeIncidentAuditSnapshot(incident);
        var previous = incident.Status;
        incident.Status = request.NewStatus;
        incident.UpdatedAt = DateTime.UtcNow;

        if (request.NewStatus == IncidentStatus.Closed)
        {
            incident.ClosedAt = DateTime.UtcNow;
            incident.ResolutionNote = request.Note;
        }

        _unitOfWork.Update(incident);

        await _unitOfWork.AddAsync(new IncidentStatusHistory
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            FromStatus = previous,
            ToStatus = request.NewStatus,
            ChangedById = userId,
            ChangedByName = userName,
            Note = request.Note ?? string.Empty,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAuditAsync(
            userId,
            "StatusChange",
            nameof(Incident),
            incident.Id.ToString(),
            oldValues,
            SerializeIncidentAuditSnapshot(incident),
            cancellationToken);

        await _notificationService.NotifyStatusChangedAsync(incident.Id, cancellationToken);

        return Result<bool>.Succeed(true, "Status updated.");
    }

    private static bool CanAccessIncident(Guid districtId, bool isSuperAdmin, IReadOnlyCollection<Guid> assignedDistrictIds)
    {
        return isSuperAdmin || assignedDistrictIds.Contains(districtId);
    }

    private static bool HasLocation(string? locationText, decimal? lat, decimal? lng)
    {
        return !string.IsNullOrWhiteSpace(locationText) || (lat.HasValue && lng.HasValue);
    }

    private static bool IsValidTransition(IncidentStatus from, IncidentStatus to, string? note)
    {
        if (from == IncidentStatus.Closed)
        {
            return false;
        }

        if (from == to)
        {
            return false;
        }

        var isForwardTransition =
            (from == IncidentStatus.Open && to == IncidentStatus.InProgress) ||
            (from == IncidentStatus.InProgress && to == IncidentStatus.Closed) ||
            (from == IncidentStatus.Open && to == IncidentStatus.Closed);

        if (!isForwardTransition)
        {
            return false;
        }

        if (to == IncidentStatus.Closed && string.IsNullOrWhiteSpace(note))
        {
            return false;
        }

        return true;
    }

    private static string SerializeIncidentAuditSnapshot(Incident incident)
    {
        var snapshot = new
        {
            incident.Id,
            incident.IncidentId,
            incident.DistrictId,
            incident.DisasterType,
            incident.Priority,
            incident.LocationGpsLat,
            incident.LocationGpsLng,
            incident.LocationText,
            incident.ReporterMobile,
            incident.Status,
            incident.ResolutionNote,
            incident.TrackingToken,
            incident.CreatedAt,
            incident.UpdatedAt,
            incident.ClosedAt
        };

        return JsonSerializer.Serialize(snapshot);
    }

    private static string SerializeIncidentCommentAuditSnapshot(IncidentComment comment)
    {
        var snapshot = new
        {
            comment.Id,
            comment.IncidentId,
            comment.AuthorId,
            comment.AuthorName,
            comment.Body,
            comment.CreatedAt
        };

        return JsonSerializer.Serialize(snapshot);
    }

    private static void ComposeSummary(IContainer container, IncidentDetailDto incident)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Summary").SemiBold().FontSize(13);
            column.Item().Element(c => KeyValueTable(c, new[]
            {
                ("Incident ID", incident.IncidentId),
                ("District", incident.DistrictName),
                ("Type", incident.DisasterType.ToString()),
                ("Priority", incident.Priority.ToString()),
                ("Status", incident.Status.ToString()),
                ("Location", incident.LocationText),
                ("Reporter Mobile", incident.ReporterMobile),
                ("Created At", incident.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"),
                ("Updated At", incident.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"),
                ("Closed At", incident.ClosedAt.HasValue ? incident.ClosedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") + " UTC" : "-"),
                ("Resolution Note", string.IsNullOrWhiteSpace(incident.ResolutionNote) ? "-" : incident.ResolutionNote)
            }));
        });
    }

    private static void ComposeStatusHistory(IContainer container, IncidentDetailDto incident)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Status History").SemiBold().FontSize(13);

            if (!incident.StatusHistory.Any())
            {
                column.Item().Text("No status history available.").FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(3);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("When (UTC)");
                    header.Cell().Element(HeaderCell).Text("From");
                    header.Cell().Element(HeaderCell).Text("To");
                    header.Cell().Element(HeaderCell).Text("Changed By");
                    header.Cell().Element(HeaderCell).Text("Note");
                });

                foreach (var h in incident.StatusHistory)
                {
                    table.Cell().Element(BodyCell).Text(h.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                    table.Cell().Element(BodyCell).Text(h.FromStatus.ToString());
                    table.Cell().Element(BodyCell).Text(h.ToStatus.ToString());
                    table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(h.ChangedByName) ? h.ChangedById : h.ChangedByName);
                    table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(h.Note) ? "-" : h.Note);
                }
            });
        });
    }

    private static void ComposeVisualEvidence(IContainer container, IncidentDetailDto incident)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Visual Evidence").SemiBold().FontSize(13);

            if (!incident.MediaFiles.Any())
            {
                column.Item().Text("No media submitted.").FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Preview");
                    header.Cell().Element(HeaderCell).Text("File Name");
                    header.Cell().Element(HeaderCell).Text("Type");
                    header.Cell().Element(HeaderCell).Text("Uploaded At (UTC)");
                });

                foreach (var media in incident.MediaFiles)
                {
                    table.Cell().Element(BodyCell).AlignCenter().Text(media.MediaType == IncidentMediaType.Video ? "[Video]" : "[Photo]");
                    table.Cell().Element(BodyCell).Text(media.FileName);
                    table.Cell().Element(BodyCell).Text(media.MediaType.ToString());
                    table.Cell().Element(BodyCell).Text(media.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            });
        });
    }

    private static void ComposeOperationalNarrative(IContainer container, IncidentDetailDto incident)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text("Operational Narrative").SemiBold().FontSize(13);
            column.Item().Element(c => ComposeStatusHistory(c, incident));
            column.Item().Element(c => ComposeComments(c, incident));
        });
    }

    private static string Csv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static void ComposeComments(IContainer container, IncidentDetailDto incident)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Comments").SemiBold().FontSize(13);

            if (!incident.Comments.Any())
            {
                column.Item().Text("No comments available.").FontColor(Colors.Grey.Darken1);
                return;
            }

            foreach (var comment in incident.Comments)
            {
                column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(inner =>
                {
                    inner.Item().Text($"{comment.AuthorName} ({comment.AuthorId})").SemiBold();
                    inner.Item().Text(comment.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC").FontSize(9).FontColor(Colors.Grey.Darken1);
                    inner.Item().Text(comment.Body);
                });
            }
        });
    }

    private static void ComposeMedia(IContainer container, IncidentDetailDto incident)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Media Files").SemiBold().FontSize(13);

            if (!incident.MediaFiles.Any())
            {
                column.Item().Text("No media files uploaded.").FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("File Name");
                    header.Cell().Element(HeaderCell).Text("Type");
                    header.Cell().Element(HeaderCell).Text("Uploaded By");
                    header.Cell().Element(HeaderCell).Text("Uploaded At (UTC)");
                });

                foreach (var media in incident.MediaFiles)
                {
                    table.Cell().Element(BodyCell).Text(media.FileName);
                    table.Cell().Element(BodyCell).Text(media.MediaType.ToString());
                    table.Cell().Element(BodyCell).Text(media.UploadedBy.ToString());
                    table.Cell().Element(BodyCell).Text(media.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            });
        });
    }

    private static void KeyValueTable(IContainer container, IReadOnlyCollection<(string Key, string Value)> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(5);
            });

            foreach (var row in rows)
            {
                table.Cell().Element(HeaderCell).Text(row.Key);
                table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(row.Value) ? "-" : row.Value);
            }
        });
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(5);
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
    }
}