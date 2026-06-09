using System.Net.Http.Json;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;

namespace BikeMate.Services;

internal static class CustomerApiClient
{
    public static async Task<bool> TryLoginDemoAccountAsync(string email, string role, CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = ApiConfig.CreateHttpClient();
            using var response = await http.PostAsJsonAsync("auth/login", new LoginRequestDto(email, "Password123!"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken);
            if (auth is null)
            {
                return false;
            }

            await SecureStorage.Default.SetAsync("access_token", auth.AccessToken);
            await SecureStorage.Default.SetAsync("primary_role", role);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<CustomerMeDto> GetCustomerAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<CustomerMeDto>(http, "customers/me", cancellationToken);
    }

    public static async Task<IReadOnlyList<ServiceRequestDto>> GetMyRequestsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ServiceRequestDto>>(http, "service-requests/my", cancellationToken);
    }

    public static async Task<ServiceRequestDto> GetRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<ServiceRequestDto>(http, $"service-requests/{requestId}", cancellationToken);
    }

    public static async Task<IReadOnlyList<PaymentDto>> GetPaymentHistoryAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<PaymentDto>>(http, "payments/history", cancellationToken);
    }

    public static async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ConversationSummaryDto>>(http, "conversations", cancellationToken);
    }

    public static async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<MessageDto>>(http, $"conversations/{conversationId}/messages", cancellationToken);
    }

    public static async Task<MessageDto> SendMessageAsync(int conversationId, string messageText, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync(
            $"conversations/{conversationId}/messages",
            new SendMessageDto(messageText, null),
            cancellationToken);
        return await ReadAsync<MessageDto>(response, cancellationToken);
    }

    public static async Task<IReadOnlyList<ShopSummaryDto>> GetShopsAsync(CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<IReadOnlyList<ShopSummaryDto>>(http, "services/shops", cancellationToken);
    }

    public static async Task<IReadOnlyList<ShopServiceDto>> SearchServicesAsync(CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<IReadOnlyList<ShopServiceDto>>(http, "services/search", cancellationToken);
    }

    public static async Task<ServiceRequestDto> CreateRequestAsync(CreateServiceRequestDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("service-requests", dto, cancellationToken);
        return await ReadAsync<ServiceRequestDto>(response, cancellationToken);
    }

    public static async Task<PaymentDto> CreateCheckoutAsync(CreateCheckoutSessionDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("payments/create-checkout-session", dto, cancellationToken);
        return await ReadAsync<PaymentDto>(response, cancellationToken);
    }

    private static async Task<T> GetAsync<T>(HttpClient http, string endpoint, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(endpoint, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
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

internal sealed record CustomerMeDto(
    int ClientId,
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? ProfileImageUrl,
    IReadOnlyList<CustomerAddressDto> Addresses,
    IReadOnlyList<MotorcycleDto> Motorcycles);
