using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Storage;

namespace BikeMate.Services;

public static class GoogleSignInService
{
    public static async Task<AuthResponseDto> SignInAsync(string role = AppRoles.Customer)
    {
        try
        {
            var state = CreateBase64UrlToken(32);
            var codeVerifier = CreateCodeVerifier();
            var startUri = BuildGoogleAuthorizationUri(state, codeVerifier);
            await EnsureBrowserAvailableAsync(startUri);

            var result = await WebAuthenticator.Default.AuthenticateAsync(
                startUri,
                new Uri(GoogleAuthConfig.RedirectUri));

            if (result.Properties.TryGetValue("error", out var error) &&
                !string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException(error);
            }

            if (!result.Properties.TryGetValue("state", out var returnedState) ||
                !string.Equals(returnedState, state, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Google sign-in returned an invalid security state. Please try again.");
            }

            if (!result.Properties.TryGetValue("code", out var code) ||
                string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("Google sign-in completed without an authorization code.");
            }

            var idToken = await ExchangeCodeForIdTokenAsync(code, codeVerifier);
            using var api = ApiConfig.CreateHttpClient();
            using var response = await api.PostAsJsonAsync("auth/google/mobile-complete", new GoogleLoginRequestDto(idToken, role));
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorBody)
                    ? "BikeMate API rejected the Google sign-in."
                    : errorBody);
            }

            return await response.Content.ReadFromJsonAsync<AuthResponseDto>()
                ?? throw new InvalidOperationException("BikeMate API did not return the Google account profile.");
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Debug.WriteLine($"Google sign-in failed: {ex}");
            throw new InvalidOperationException(
                "Google sign-in could not start or complete. Check that the BikeMate API is running, the Google web OAuth client is configured on the API, and Chrome is enabled on the emulator.",
                ex);
        }
    }

    private static Uri BuildGoogleAuthorizationUri(string state, string codeVerifier)
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = GoogleAuthConfig.AndroidClientId,
            ["redirect_uri"] = GoogleAuthConfig.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = state,
            ["prompt"] = "select_account",
            ["code_challenge"] = CreateCodeChallenge(codeVerifier),
            ["code_challenge_method"] = "S256"
        };

        var queryString = string.Join("&", query.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));
        return new Uri($"https://accounts.google.com/o/oauth2/v2/auth?{queryString}");
    }

    private static async Task<string> ExchangeCodeForIdTokenAsync(string code, string codeVerifier)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var response = await http.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = GoogleAuthConfig.AndroidClientId,
                ["code"] = code,
                ["code_verifier"] = codeVerifier,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = GoogleAuthConfig.RedirectUri
            }));

        var payload = await response.Content.ReadAsStringAsync();
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

    private static string CreateCodeVerifier()
    {
        return CreateBase64UrlToken(64);
    }

    private static string CreateCodeChallenge(string codeVerifier)
    {
        return Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
    }

    private static string CreateBase64UrlToken(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Base64Url(bytes);
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static async Task StoreAuthAsync(AuthResponseDto auth)
    {
        var role = PickPrimaryRole(auth.User.Roles);
        await SecureStorage.Default.SetAsync("access_token", auth.AccessToken);
        await SecureStorage.Default.SetAsync("primary_role", role);
        await SecureStorage.Default.SetAsync("user_id", auth.User.UserId.ToString());
    }

    public static string PickPrimaryRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(AppRoles.SystemAdmin)) return AppRoles.SystemAdmin;
        if (roles.Contains(AppRoles.ShopAdmin)) return AppRoles.ShopAdmin;
        if (roles.Contains(AppRoles.Mechanic)) return AppRoles.Mechanic;
        return AppRoles.Customer;
    }

    private static async Task EnsureBrowserAvailableAsync(Uri authUri)
    {
        try
        {
            if (!await Launcher.Default.CanOpenAsync(authUri))
            {
                throw new InvalidOperationException("This Android emulator cannot open Google sign-in because no browser is available. Use a Google Play emulator image with Chrome enabled, then try again.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Google browser preflight failed: {ex}");
            throw new InvalidOperationException("This Android emulator cannot open Google sign-in. Check that Chrome or another browser is installed and enabled.", ex);
        }
    }
}
