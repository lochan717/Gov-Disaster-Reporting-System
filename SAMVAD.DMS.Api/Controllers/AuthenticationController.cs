using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMVAD.DMS.Application.DTOs.Auth;
using SAMVAD.DMS.Application.Services;
using SAMVAD.DMS.Shared.Models;

namespace SAMVAD.DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponseDto<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<LoginResponseDto>.Error("Invalid request data."));
        }

        var result = await _authenticationService.LoginAsync(request);
        if (!result.IsSuccess)
        {
            return Unauthorized(ApiResponseDto<LoginResponseDto>.Error(result.Message ?? "Invalid login request."));
        }

        return Ok(ApiResponseDto<LoginResponseDto>.Ok(result.Data));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponseDto<bool>>> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Error("Invalid request data."));
        }

        var result = await _authenticationService.ForgotPasswordAsync(request);
        return Ok(result.IsSuccess
            ? ApiResponseDto<bool>.Ok(true, result.Message)
            : ApiResponseDto<bool>.Error(result.Message ?? "Unable to process request."));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponseDto<bool>>> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto<bool>.Error("Invalid request data."));
        }

        var result = await _authenticationService.ResetPasswordAsync(request);
        return result.IsSuccess
            ? Ok(ApiResponseDto<bool>.Ok(true, result.Message))
            : BadRequest(ApiResponseDto<bool>.Error(result.Message ?? "Unable to reset password."));
    }
}