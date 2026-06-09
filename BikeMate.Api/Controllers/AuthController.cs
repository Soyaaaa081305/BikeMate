using BikeMate.Api.Helpers;
using BikeMate.Api.Services;
using BikeMate.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto dto, CancellationToken cancellationToken)
    {
        return Ok(await authService.RegisterAsync(dto, cancellationToken));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto dto, CancellationToken cancellationToken)
    {
        return Ok(await authService.LoginAsync(dto, cancellationToken));
    }

    [HttpPost("google-login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> GoogleLogin(GoogleLoginRequestDto dto, CancellationToken cancellationToken)
    {
        return Ok(await authService.GoogleLoginAsync(dto, cancellationToken));
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequestDto dto, CancellationToken cancellationToken)
    {
        await authService.VerifyOtpAsync(dto, cancellationToken);
        return Ok(new { message = "OTP verified." });
    }

    [HttpPost("resend-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendOtp(ResendOtpRequestDto dto, CancellationToken cancellationToken)
    {
        await authService.ResendOtpAsync(dto, cancellationToken);
        return Ok(new { message = "OTP sent." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto dto, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(dto, cancellationToken);
        return Ok(new { message = "Password reset instructions were sent if the email exists." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto dto, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(dto, cancellationToken);
        return Ok(new { message = "Password updated." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken cancellationToken)
    {
        return Ok(await authService.GetMeAsync(User.GetUserId(), cancellationToken));
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "Token discarded on client." });
    }
}
