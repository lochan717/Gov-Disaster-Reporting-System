using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SAMVAD.DMS.Application.DTOs.User;
using SAMVAD.DMS.Domain.Entities;
using SAMVAD.DMS.Domain.Interfaces;
using SAMVAD.DMS.Shared.Models;
using System.Text.Json;

namespace SAMVAD.DMS.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly string _webBaseUrl;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _emailService = emailService;
        _webBaseUrl = (configuration["WebBaseUrl"] ?? "https://localhost:7002").TrimEnd('/');
    }

    public Task<Result<AdminFormOptionsDto>> GetAdminFormOptionsAsync(CancellationToken cancellationToken = default)
    {
        var roles = _roleManager.Roles
            .Select(x => x.Name)
            .OfType<string>()
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x)
            .ToArray();

        var districts = _unitOfWork.Query<District>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new AdminDistrictOptionDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToArray();

        var payload = new AdminFormOptionsDto
        {
            RoleOptions = roles,
            DistrictOptions = districts
        };

        return Task.FromResult(Result<AdminFormOptionsDto>.Succeed(payload));
    }

    public async Task<Result<AdminDetailDto>> CreateAdminAsync(CreateAdminDto request, string createdByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _roleManager.RoleExistsAsync(request.RoleLabel))
            {
                return Result<AdminDetailDto>.Fail("Selected role does not exist.");
            }

            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing is not null)
            {
                return Result<AdminDetailDto>.Fail("Email already in use.");
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.Name,
                RoleLabel = request.RoleLabel,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, "Temp@12345!");
            if (!createResult.Succeeded)
            {
                return Result<AdminDetailDto>.Fail(string.Join(' ', createResult.Errors.Select(x => x.Description)));
            }

            await _userManager.AddToRoleAsync(user, request.RoleLabel);

            foreach (var districtId in request.AssignedDistrictIds.Distinct())
            {
                await _unitOfWork.AddAsync(new ApplicationUserDistrict
                {
                    ApplicationUserId = user.Id,
                    DistrictId = districtId
                }, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var createdDistrictIds = request.AssignedDistrictIds.Distinct().ToArray();
            await _auditService.LogAuditAsync(
                createdByUserId,
                "Create",
                nameof(ApplicationUser),
                user.Id,
                null,
                SerializeUserAuditSnapshot(user, createdDistrictIds),
                cancellationToken);

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = $"{_webBaseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(resetToken)}";

            user.PasswordResetRequestedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            try
            {
                await _emailService.SendTemplatedAsync(
                    user.Email!,
                    "Set Your Password",
                    "SetPasswordInvite",
                    new Dictionary<string, string>
                    {
                        ["AppName"] = "SAMVAD DMS",
                        ["RecipientName"] = user.FullName,
                        ["ResetLink"] = link
                    },
                    cancellationToken);

                return Result<AdminDetailDto>.Succeed(await MapAdminDetailAsync(user.Id, cancellationToken), "Admin created and invite email sent.");
            }
            catch (Exception)
            {
                return Result<AdminDetailDto>.Succeed(await MapAdminDetailAsync(user.Id, cancellationToken), "Admin created, but invite email could not be sent. Check Email SMTP settings.");
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public async Task<Result<AdminDetailDto>> GetAdminByIdAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(adminId);
        if (user is null)
        {
            return Result<AdminDetailDto>.Fail("Admin not found.");
        }

        return Result<AdminDetailDto>.Succeed(await MapAdminDetailAsync(adminId, cancellationToken));
    }

    public Task<Result<PaginatedResult<AdminListItemDto>>> GetAdminsAsync(int page, int pageSize, string? searchTerm, bool? isActive, string? roleLabel, Guid? districtId, CancellationToken cancellationToken = default)
    {
        var currentPage = page < 1 ? 1 : page;
        var currentPageSize = pageSize < 1 ? 20 : pageSize;

        var users = _unitOfWork.Query<ApplicationUser>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            users = users.Where(x =>
                x.FullName.ToLower().Contains(term) ||
                (x.Email != null && x.Email.ToLower().Contains(term)));
        }

        if (isActive.HasValue)
        {
            users = users.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(roleLabel))
        {
            var normalizedRole = roleLabel.Trim();
            users = users.Where(x => x.RoleLabel == normalizedRole);
        }

        if (districtId.HasValue)
        {
            var selectedDistrictId = districtId.Value;
            users = users.Where(x => _unitOfWork.Query<ApplicationUserDistrict>()
                .Any(ud => ud.ApplicationUserId == x.Id && ud.DistrictId == selectedDistrictId));
        }

        var districtMap = _unitOfWork.Query<ApplicationUserDistrict>()
            .Join(_unitOfWork.Query<District>(), ud => ud.DistrictId, d => d.Id, (ud, d) => new { ud.ApplicationUserId, DistrictName = d.Name })
            .ToList();

        var totalCount = users.Count();
        var items = users
            .OrderBy(x => x.FullName)
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .ToList()
            .Select(x => new AdminListItemDto
            {
                Id = x.Id,
                Name = x.FullName,
                Email = x.Email ?? string.Empty,
                RoleLabel = x.RoleLabel,
                IsActive = x.IsActive,
                AssignedDistricts = districtMap.Where(d => d.ApplicationUserId == x.Id).Select(d => d.DistrictName).ToArray()
            })
            .ToArray();

        var payload = PaginatedResult<AdminListItemDto>.Create(items, totalCount, currentPage, currentPageSize);
        return Task.FromResult(Result<PaginatedResult<AdminListItemDto>>.Succeed(payload));
    }

    public async Task<Result<bool>> ToggleAdminStatusAsync(string adminId, string updatedByUserId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(adminId);
        if (user is null)
        {
            return Result<bool>.Fail("Admin not found.");
        }

        var oldValues = SerializeUserAuditSnapshot(user);
        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAuditAsync(
            updatedByUserId,
            "ToggleStatus",
            nameof(ApplicationUser),
            user.Id,
            oldValues,
            SerializeUserAuditSnapshot(user),
            cancellationToken);

        return Result<bool>.Succeed(true, "Admin status updated.");
    }

    public async Task<Result<bool>> TriggerResetPasswordAsync(string adminId, ResetUserPasswordDto request, string requestedByUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result<bool>.Fail("New password is required.");
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return Result<bool>.Fail("Confirm password does not match.");
        }

        var user = await _userManager.FindByIdAsync(adminId);
        if (user is null)
        {
            return Result<bool>.Fail("User not found.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            return Result<bool>.Fail(string.Join(' ', resetResult.Errors.Select(x => x.Description)));
        }

        user.PasswordResetRequestedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAuditAsync(
            requestedByUserId,
            "ResetPassword",
            nameof(ApplicationUser),
            user.Id,
            null,
            "Password reset completed by super admin.",
            cancellationToken);

        return Result<bool>.Succeed(true, "Password reset successfully.");
    }

    public async Task<Result<AdminDetailDto>> UpdateAdminAsync(string adminId, UpdateAdminDto request, string updatedByUserId, CancellationToken cancellationToken = default)
    {
        if (!await _roleManager.RoleExistsAsync(request.RoleLabel))
        {
            return Result<AdminDetailDto>.Fail("Selected role does not exist.");
        }

        var user = await _userManager.FindByIdAsync(adminId);
        if (user is null)
        {
            return Result<AdminDetailDto>.Fail("Admin not found.");
        }

        var previousDistrictIds = _unitOfWork.Query<ApplicationUserDistrict>()
            .Where(x => x.ApplicationUserId == user.Id)
            .Select(x => x.DistrictId)
            .ToArray();
        var oldValues = SerializeUserAuditSnapshot(user, previousDistrictIds);

        user.FullName = request.Name.Trim();
        user.RoleLabel = request.RoleLabel.Trim();
        await _userManager.UpdateAsync(user);

        var existingRoles = await _userManager.GetRolesAsync(user);
        if (existingRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, existingRoles);
        }

        await _userManager.AddToRoleAsync(user, request.RoleLabel);

        var currentAssignments = _unitOfWork.Query<ApplicationUserDistrict>()
            .Where(x => x.ApplicationUserId == user.Id)
            .ToList();

        foreach (var assignment in currentAssignments)
        {
            _unitOfWork.Remove(assignment);
        }

        var updatedDistrictIds = request.AssignedDistrictIds.Distinct().ToArray();
        foreach (var districtId in updatedDistrictIds)
        {
            await _unitOfWork.AddAsync(new ApplicationUserDistrict
            {
                ApplicationUserId = user.Id,
                DistrictId = districtId
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAuditAsync(
            updatedByUserId,
            "Update",
            nameof(ApplicationUser),
            user.Id,
            oldValues,
            SerializeUserAuditSnapshot(user, updatedDistrictIds),
            cancellationToken);

        return Result<AdminDetailDto>.Succeed(await MapAdminDetailAsync(user.Id, cancellationToken));
    }

    private Task<AdminDetailDto> MapAdminDetailAsync(string userId, CancellationToken cancellationToken)
    {
        var user = _unitOfWork.Query<ApplicationUser>().First(x => x.Id == userId);
        var districtIds = _unitOfWork.Query<ApplicationUserDistrict>()
            .Where(x => x.ApplicationUserId == userId)
            .Select(x => x.DistrictId)
            .ToArray();

        return Task.FromResult(new AdminDetailDto
        {
            Id = user.Id,
            Name = user.FullName,
            Email = user.Email ?? string.Empty,
            RoleLabel = user.RoleLabel,
            IsActive = user.IsActive,
            CreatedOn = user.CreatedOn,
            LastLogin = user.LastLogin,
            AssignedDistrictIds = districtIds
        });
    }

    private static string SerializeUserAuditSnapshot(ApplicationUser user, IReadOnlyCollection<Guid>? assignedDistrictIds = null)
    {
        var snapshot = new
        {
            user.Id,
            user.UserName,
            user.Email,
            FullName = user.FullName,
            RoleLabel = user.RoleLabel,
            user.IsActive,
            user.CreatedOn,
            user.LastLogin,
            user.PasswordResetRequestedAt,
            AssignedDistrictIds = assignedDistrictIds ?? Array.Empty<Guid>()
        };

        return JsonSerializer.Serialize(snapshot);
    }
}