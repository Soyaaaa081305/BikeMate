using BikeMate.Api.Helpers;
using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IAuthService authService,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : ControllerBase
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
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> GoogleLogin(GoogleLoginRequestDto dto, CancellationToken cancellationToken)
    {
        return Ok(await authService.GoogleLoginAsync(dto, cancellationToken));
    }

    [HttpPost("google/mobile-complete")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> GoogleMobileComplete(GoogleLoginRequestDto dto, CancellationToken cancellationToken)
    {
        return Ok(await authService.GoogleLoginAsync(dto, cancellationToken));
    }

    [HttpGet("google/start")]
    [AllowAnonymous]
    public IActionResult StartGoogleLogin([FromQuery] string? role = null)
    {
        var clientId = GetGoogleWebClientId();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return RedirectToMobileGoogleError("Google sign-in is not configured on the API. Set GoogleAuth:WebClientId.");
        }

        var clientSecret = configuration["GoogleAuth:WebClientSecret"] ?? configuration["GoogleAuth:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientSecret) ||
            clientSecret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToMobileGoogleError("Google sign-in is not configured on the API. Set GoogleAuth:WebClientSecret.");
        }

        var redirectUri = GetGoogleRedirectUri();
        var selectedRole = AppRoles.All.Contains(role ?? string.Empty) ? role! : AppRoles.Customer;
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = selectedRole,
            ["prompt"] = "select_account"
        };

        return Redirect(QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", query));
    }

    [HttpGet("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return RedirectToMobileGoogleError($"Google rejected sign-in: {error}");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return RedirectToMobileGoogleError("Google did not return an authorization code.");
        }

        try
        {
            var idToken = await ExchangeGoogleCodeForIdTokenAsync(code, cancellationToken);
            var role = AppRoles.All.Contains(state ?? string.Empty) ? state! : AppRoles.Customer;
            var auth = await authService.GoogleLoginAsync(new GoogleLoginRequestDto(idToken, role), cancellationToken);
            var callback = QueryHelpers.AddQueryString(GetMobileGoogleCallbackUri(), new Dictionary<string, string?>
            {
                ["access_token"] = auth.AccessToken,
                ["expires_at"] = auth.ExpiresAt.ToString("O")
            });

            return Redirect(callback);
        }
        catch (Exception ex)
        {
            return RedirectToMobileGoogleError(ex.Message);
        }
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
        return Ok(new { message = "If that email exists, BikeMate sent a reset code." });
    }

    [HttpPost("verify-password-reset-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPasswordResetOtp(VerifyPasswordResetOtpRequestDto dto, CancellationToken cancellationToken)
    {
        await authService.VerifyPasswordResetOtpAsync(dto, cancellationToken);
        return Ok(new { message = "Reset code verified." });
    }

    [HttpPost("resend-password-reset-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendPasswordResetOtp(ResendPasswordResetOtpRequestDto dto, CancellationToken cancellationToken)
    {
        await authService.ResendPasswordResetOtpAsync(dto, cancellationToken);
        return Ok(new { message = "If that email exists, BikeMate sent a new reset code." });
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

    private async Task<string> ExchangeGoogleCodeForIdTokenAsync(string code, CancellationToken cancellationToken)
    {
        var clientId = GetGoogleWebClientId();
        var clientSecret = configuration["GoogleAuth:WebClientSecret"] ?? configuration["GoogleAuth:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            clientSecret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Google sign-in is not configured on the API. Set GoogleAuth:WebClientId and GoogleAuth:WebClientSecret.");
        }

        using var response = await httpClientFactory.CreateClient().PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = GetGoogleRedirectUri()
            }),
            cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Google token exchange failed: {payload}");
        }

        using var document = JsonDocument.Parse(payload);
        return document.RootElement.TryGetProperty("id_token", out var idToken) &&
               !string.IsNullOrWhiteSpace(idToken.GetString())
            ? idToken.GetString()!
            : throw new InvalidOperationException("Google did not return an ID token.");
    }

    private string? GetGoogleWebClientId()
    {
        var clientId = configuration["GoogleAuth:WebClientId"] ?? configuration["GoogleAuth:ClientId"];
        return string.IsNullOrWhiteSpace(clientId) || clientId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
            ? null
            : clientId;
    }

    private string GetGoogleRedirectUri()
    {
        var configured = configuration["GoogleAuth:RedirectUri"];
        if (!string.IsNullOrWhiteSpace(configured) && !configured.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            return configured;
        }

        var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
        var host = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.Value;
        return $"{scheme}://{host}/api/auth/google/callback";
    }

    private string GetMobileGoogleCallbackUri()
    {
        var configured = configuration["GoogleAuth:MobileCallbackUri"];
        return string.IsNullOrWhiteSpace(configured) || configured.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
            ? "bikemate://auth/google"
            : configured;
    }

    private IActionResult RedirectToMobileGoogleError(string message)
    {
        var callback = QueryHelpers.AddQueryString(GetMobileGoogleCallbackUri(), "error", message);
        return Redirect(callback);
    }
}
