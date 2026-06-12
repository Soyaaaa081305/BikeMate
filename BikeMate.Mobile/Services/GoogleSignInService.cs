using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using Microsoft.Maui.Authentication;

namespace BikeMate.Services;

public static class GoogleSignInService
{
    public static async Task<AuthResponseDto> SignInAsync(string role = AppRoles.Customer)
    {
        var codeVerifier = CreateCodeVerifier();
        var state = CreateCodeVerifier();
        var codeChallenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        var authUrl =
            "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(GoogleAuthConfig.AndroidClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(GoogleAuthConfig.RedirectUri)}" +
            "&response_type=code" +
            "&scope=openid%20email%20profile" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            "&code_challenge_method=S256" +
            $"&state={Uri.EscapeDataString(state)}" +
            "&prompt=select_account";

        var result = await WebAuthenticator.Default.AuthenticateAsync(
            new Uri(authUrl),
            new Uri(GoogleAuthConfig.RedirectUri));

        if (!result.Properties.TryGetValue("state", out var returnedState) ||
            !string.Equals(returnedState, state, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Google sign-in state did not match.");
        }

        if (!result.Properties.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Google did not return an authorization code.");
        }

        var idToken = await ExchangeCodeForIdTokenAsync(code, codeVerifier);
        using var api = ApiConfig.CreateHttpClient();
        using var response = await api.PostAsJsonAsync("auth/google", new GoogleLoginRequestDto(idToken, role));
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        }

        return await response.Content.ReadFromJsonAsync<AuthResponseDto>()
            ?? throw new InvalidOperationException("Google login did not return an auth response.");
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

    private static async Task<string> ExchangeCodeForIdTokenAsync(string code, string codeVerifier)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
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
        return document.RootElement.TryGetProperty("id_token", out var idToken)
            ? idToken.GetString() ?? throw new InvalidOperationException("Google did not return an ID token.")
            : throw new InvalidOperationException("Google did not return an ID token.");
    }

    private static string CreateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal);
    }
}
