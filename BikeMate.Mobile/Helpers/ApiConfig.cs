using System.Net;
using System.Net.Http.Headers;

namespace BikeMate.Helpers;

public enum StoredSessionStatus
{
    Missing,
    Valid,
    Rejected,
    Unavailable
}

public sealed class ApiSessionExpiredException(string message) : InvalidOperationException(message);

public static class ApiConfig
{
    public const string BaseUrl =
#if ANDROID
        "https://10.0.2.2:5001/api/";
#else
        "https://localhost:5001/api/";
#endif

    public static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();

        if (BaseUrl.Contains("10.0.2.2", StringComparison.OrdinalIgnoreCase) ||
            BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };

        if (BaseUrl.Contains("ngrok-free.dev", StringComparison.OrdinalIgnoreCase))
        {
            http.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
        }

        return http;
    }

    public static async Task<HttpClient> CreateAuthorizedHttpClientAsync()
    {
        var http = CreateHttpClient();
        var token = await SecureStorage.Default.GetAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            http.Dispose();
            await AppNavigation.HandleUnauthorizedAsync();
            throw new ApiSessionExpiredException("Your BikeMate session has ended. Please sign in again.");
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    public static async Task<StoredSessionStatus> ValidateStoredSessionAsync(CancellationToken cancellationToken = default)
    {
        var token = await SecureStorage.Default.GetAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            return StoredSessionStatus.Missing;
        }

        try
        {
            using var http = CreateHttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.GetAsync("auth/me", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return StoredSessionStatus.Valid;
            }

            return response.StatusCode == HttpStatusCode.Unauthorized
                ? StoredSessionStatus.Rejected
                : StoredSessionStatus.Unavailable;
        }
        catch (HttpRequestException)
        {
            return StoredSessionStatus.Unavailable;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StoredSessionStatus.Unavailable;
        }
    }

    public static async Task ThrowIfAuthenticationFailedAsync(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return;
        }

        await AppNavigation.HandleUnauthorizedAsync();
        throw new ApiSessionExpiredException("Your BikeMate session has expired. Please sign in again.");
    }
}
