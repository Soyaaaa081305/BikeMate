namespace BikeMate.Helpers;

public static class ApiConfig
{
    public const string BaseUrl =
#if ANDROID
        "https://hungrily-imagines-suffering.ngrok-free.dev/api/";
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
        if (!string.IsNullOrWhiteSpace(token))
        {
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return http;
    }
}
