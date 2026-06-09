namespace BikeMate.Helpers;

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

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };
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
