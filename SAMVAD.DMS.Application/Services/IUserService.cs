using SAMVAD.DMS.Application.DTOs.User;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Application.Services;

public interface IUserService
{
    Task<Result<AdminFormOptionsDto>> GetAdminFormOptionsAsync(CancellationToken cancellationToken = default);
    Task<Result<PaginatedResult<AdminListItemDto>>> GetAdminsAsync(int page, int pageSize, string? searchTerm, bool? isActive, string? roleLabel, Guid? districtId, CancellationToken cancellationToken = default);
    Task<Result<AdminDetailDto>> GetAdminByIdAsync(string adminId, CancellationToken cancellationToken = default);
    Task<Result<AdminDetailDto>> CreateAdminAsync(CreateAdminDto request, string createdByUserId, CancellationToken cancellationToken = default);
    Task<Result<AdminDetailDto>> UpdateAdminAsync(string adminId, UpdateAdminDto request, string updatedByUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ToggleAdminStatusAsync(string adminId, string updatedByUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> TriggerResetPasswordAsync(string adminId, ResetUserPasswordDto request, string requestedByUserId, CancellationToken cancellationToken = default);
}