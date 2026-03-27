using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SAMVAD.DMS.Application.DTOs.Auth;
using SAMVAD.DMS.Domain.Entities;
using SAMVAD.DMS.Domain.Interfaces;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _userManager = userManager;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Result<bool>.Succeed(true, "If the account exists, a reset link has been sent.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var webBaseUrl = (_configuration["WebBaseUrl"] ?? "https://localhost:7002").TrimEnd('/');
        var resetLink = $"{webBaseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

        user.PasswordResetRequestedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _emailService.SendTemplatedAsync(
            user.Email!,
            "SAMVAD DMS Password Reset",
            "ForgotPassword",
            new Dictionary<string, string>
            {
                ["AppName"] = "SAMVAD DMS",
                ["RecipientName"] = string.IsNullOrWhiteSpace(user.FullName) ? "User" : user.FullName,
                ["ResetLink"] = resetLink
            },
            cancellationToken);

        return Result<bool>.Succeed(true, "If the account exists, a reset link has been sent.");
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Result<LoginResponseDto>.Fail("Invalid email or password.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result<LoginResponseDto>.Fail("Account is temporarily locked. Please try again later.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return Result<LoginResponseDto>.Fail("Invalid email or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var assignedDistrictIds = _unitOfWork.Query<ApplicationUserDistrict>()
            .Where(x => x.ApplicationUserId == user.Id)
            .Select(x => x.DistrictId)
            .ToArray();

        var token = GenerateJwtToken(user, roles, assignedDistrictIds);

        user.LastLogin = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var response = new LoginResponseDto
        {
            Id = user.Id,
            Username = user.UserName ?? user.Email ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Token = token,
            RoleLabel = user.RoleLabel,
            Roles = roles.ToArray(),
            AssignedDistrictIds = assignedDistrictIds
        };

        return Result<LoginResponseDto>.Succeed(response);
    }

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Result<bool>.Fail("Invalid password reset request.");
        }

        if (!user.PasswordResetRequestedAt.HasValue || DateTime.UtcNow - user.PasswordResetRequestedAt.Value > TimeSpan.FromMinutes(30))
        {
            return Result<bool>.Fail("Reset link is expired. Please request a new one.");
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(" ", resetResult.Errors.Select(x => x.Description));
            return Result<bool>.Fail(string.IsNullOrWhiteSpace(errors) ? "Unable to reset password." : errors);
        }

        user.PasswordResetRequestedAt = null;
        await _userManager.UpdateAsync(user);

        await _emailService.SendTemplatedAsync(
            user.Email!,
            "SAMVAD DMS Password Changed",
            "ResetPasswordSuccess",
            new Dictionary<string, string>
            {
                ["AppName"] = "SAMVAD DMS",
                ["RecipientName"] = string.IsNullOrWhiteSpace(user.FullName) ? "User" : user.FullName
            },
            cancellationToken);

        return Result<bool>.Succeed(true, "Password has been reset.");
    }

    private string GenerateJwtToken(ApplicationUser user, IEnumerable<string> roles, IReadOnlyCollection<Guid> districtIds)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("FullName", user.FullName),
            new("RoleLabel", user.RoleLabel),
            new("AssignedDistrictIds", string.Join(',', districtIds))
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = _configuration["Jwt:Key"] ?? "samvad-dms-super-secret-key-change-in-production";
        var issuer = _configuration["Jwt:Issuer"] ?? "SAMVAD.DMS.Api";
        var audience = _configuration["Jwt:Audience"] ?? "SAMVAD.DMS.Web";
        var expiryHours = int.TryParse(_configuration["Jwt:ExpiryHours"], out var value) ? value : 1;

        var tokenDescriptor = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}