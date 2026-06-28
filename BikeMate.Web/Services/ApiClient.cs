using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BikeMate.Core.DTOs;

namespace BikeMate.Web.Services;

public sealed class ApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("auth/login", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Login response was empty.");
    }

    public Task<UserProfileDto> GetMeAsync(string token, CancellationToken cancellationToken = default)
        => GetAsync<UserProfileDto>("auth/me", token, cancellationToken);

    public Task<AdminDashboardDto> GetAdminDashboardAsync(string token, CancellationToken cancellationToken = default)
        => GetAsync<AdminDashboardDto>("admin/dashboard", token, cancellationToken);

    public Task<IReadOnlyList<CustomerRow>> GetAdminCustomersAsync(string token, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<CustomerRow>>("admin/customers", token, cancellationToken);

    public Task<IReadOnlyList<MechanicRow>> GetAdminMechanicsAsync(string token, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<MechanicRow>>("admin/mechanics", token, cancellationToken);

    public Task<IReadOnlyList<ShopRow>> GetAdminShopsAsync(string token, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<ShopRow>>("admin/shops", token, cancellationToken);

    public Task<IReadOnlyList<ServiceRequestDto>> GetAdminServiceRequestsAsync(string token, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<ServiceRequestDto>>("admin/service-requests", token, cancellationToken);

    private async Task<T> GetAsync<T>(string path, string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"The {path} response was empty.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(body)
            ? $"Request failed with HTTP {(int)response.StatusCode}."
            : body);
    }
}

public sealed record CustomerRow(
    int CustomerId,
    int UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string AccountStatus,
    DateTime CreatedAt);

public sealed record MechanicRow(
    int MechanicId,
    int UserId,
    string FullName,
    bool IsVerified,
    string AvailabilityStatus,
    decimal AverageRating,
    int TotalCompletedJobs);

public sealed record ShopRow(
    int ShopId,
    int OwnerUserId,
    string ShopName,
    string? City,
    string? Province,
    string ShopStatus);
