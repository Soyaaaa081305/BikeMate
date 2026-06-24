using System.Net.Http.Json;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;

namespace BikeMate.Services;

internal static class RiderApiClient
{
    public static async Task<RiderDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<RiderDashboardDto>(http, "rider/dashboard", cancellationToken);
    }

    public static async Task<IReadOnlyList<ServiceRequestDto>> GetIncomingAsync(decimal radiusKm = 8m, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ServiceRequestDto>>(http, $"rider/requests/incoming?radiusKm={radiusKm}", cancellationToken);
    }

    public static async Task<IReadOnlyList<ServiceRequestDto>> GetEmergencyAsync(decimal radiusKm = 12m, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ServiceRequestDto>>(http, $"rider/requests/emergency?radiusKm={radiusKm}", cancellationToken);
    }

    public static async Task<ServiceRequestDto?> GetCurrentJobAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.GetAsync("rider/jobs/current", cancellationToken);
        await ApiConfig.ThrowIfAuthenticationFailedAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"API request failed with {(int)response.StatusCode}."
                : error);
        }

        return await response.Content.ReadFromJsonAsync<ServiceRequestDto?>(cancellationToken);
    }

    public static async Task<ServiceRequestDto> GetJobAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<ServiceRequestDto>(http, $"rider/jobs/{requestId}", cancellationToken);
    }

    public static async Task<IReadOnlyList<ServiceRequestDto>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ServiceRequestDto>>(http, "rider/jobs/history", cancellationToken);
    }

    public static async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ConversationSummaryDto>>(http, "conversations", cancellationToken);
    }

    public static async Task<MechanicProfileDto> GoOnlineAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync("rider/status/online", null, cancellationToken);
        return await ReadAsync<MechanicProfileDto>(response, cancellationToken);
    }

    public static async Task<MechanicProfileDto> GoOfflineAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync("rider/status/offline", null, cancellationToken);
        return await ReadAsync<MechanicProfileDto>(response, cancellationToken);
    }

    public static async Task<ServiceRequestDto> AcceptAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync($"rider/jobs/{requestId}/accept", null, cancellationToken);
        return await ReadAsync<ServiceRequestDto>(response, cancellationToken);
    }

    public static async Task<ServiceRequestDto> RejectAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync($"rider/jobs/{requestId}/reject", null, cancellationToken);
        return await ReadAsync<ServiceRequestDto>(response, cancellationToken);
    }

    public static async Task<ServiceRequestDto> UpdateStatusAsync(int requestId, string status, string? notes = null, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync($"rider/jobs/{requestId}/status", new UpdateRequestStatusDto(status, notes), cancellationToken);
        return await ReadAsync<ServiceRequestDto>(response, cancellationToken);
    }

    public static async Task<LiveLocationDto> UpdateLocationAsync(int? requestId, decimal latitude, decimal longitude, decimal? accuracyMeters = null, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("rider/location", new LocationUpdateDto(requestId, null, latitude, longitude, accuracyMeters), cancellationToken);
        return await ReadAsync<LiveLocationDto>(response, cancellationToken);
    }

    private static async Task<T> GetAsync<T>(HttpClient http, string endpoint, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(endpoint, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await ApiConfig.ThrowIfAuthenticationFailedAsync(response);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"API request failed with {(int)response.StatusCode}."
                : error);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty response.");
    }
}

internal sealed record RiderDashboardDto(
    MechanicProfileDto Profile,
    string AvailabilityStatus,
    ServiceRequestDto? ActiveJob,
    int IncomingRequests,
    int EmergencyRequests,
    decimal TotalEarnings,
    decimal AverageRating,
    int TotalCompletedJobs,
    int UnreadNotifications);
