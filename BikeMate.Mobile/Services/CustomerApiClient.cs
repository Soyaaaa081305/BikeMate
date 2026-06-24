using System.Net.Http.Headers;
using System.Net.Http.Json;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using Microsoft.Maui.Storage;

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

    public static async Task UpdateCustomerAsync(UpsertCustomerProfileDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync("customers/me", dto, cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task UpdateCustomerProfileImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync(
            "customers/me/profile-image",
            new UploadMediaDto(imageUrl, "profile_photo", "Customer profile photo"),
            cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task UpdateCustomerValidIdAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync(
            "customers/me/valid-id",
            new UploadMediaDto(imageUrl, "valid_id", "Customer valid ID"),
            cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task DeleteCustomerAccountAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.DeleteAsync("customers/me", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await ReadAsync<object>(response, cancellationToken);
        }
    }

    public static async Task<CustomerAddressDto> UpsertAddressAsync(CustomerAddressDto? existing, UpsertCustomerAddressDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = existing is null
            ? await http.PostAsJsonAsync("customers/address", dto, cancellationToken)
            : await http.PutAsJsonAsync($"customers/address/{existing.AddressId}", dto, cancellationToken);
        return await ReadAsync<CustomerAddressDto>(response, cancellationToken);
    }

    public static async Task<MotorcycleDto> UpsertMotorcycleAsync(MotorcycleDto? existing, UpsertMotorcycleDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = existing is null
            ? await http.PostAsJsonAsync("customers/motorcycles", dto, cancellationToken)
            : await http.PutAsJsonAsync($"customers/motorcycles/{existing.MotorcycleId}", dto, cancellationToken);
        return await ReadAsync<MotorcycleDto>(response, cancellationToken);
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

    public static async Task<PaymentDto?> GetLatestPaymentForRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
            return await GetAsync<PaymentDto>(http, $"payments/request/{requestId}/latest", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<PaymentDto> RefreshPaymentAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsync($"payments/{paymentId}/refresh", null, cancellationToken);
        return await ReadAsync<PaymentDto>(response, cancellationToken);
    }

    public static async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<ConversationSummaryDto>>(http, "conversations", cancellationToken);
    }

    public static async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<NotificationDto>>(http, "notifications", cancellationToken);
    }

    public static async Task MarkNotificationReadAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync($"notifications/{notificationId}/read", null, cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        return await GetAsync<IReadOnlyList<MessageDto>>(http, $"conversations/{conversationId}/messages", cancellationToken);
    }

    public static async Task MarkConversationReadAsync(int conversationId, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsync($"conversations/{conversationId}/read-all", null, cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task<UploadedFileDto> UploadFileAsync(FileResult file, string folder = "chat", CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        await using var stream = await file.OpenReadAsync();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(file));
        content.Add(streamContent, "file", file.FileName);
        content.Add(new StringContent(folder), "folder");

        using var response = await http.PostAsync("files/upload", content, cancellationToken);
        return await ReadAsync<UploadedFileDto>(response, cancellationToken);
    }

    public static async Task<MessageDto> SendMessageAsync(int conversationId, string messageText, string? attachmentUrl = null, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync(
            $"conversations/{conversationId}/messages",
            new SendMessageDto(messageText, attachmentUrl),
            cancellationToken);
        return await ReadAsync<MessageDto>(response, cancellationToken);
    }

    public static async Task<IReadOnlyList<ShopSummaryDto>> GetShopsAsync(CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<IReadOnlyList<ShopSummaryDto>>(http, "services/shops", cancellationToken);
    }

    public static async Task<ShopDetailsDto> GetShopDetailsAsync(int shopId, CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<ShopDetailsDto>(http, $"shops/{shopId}", cancellationToken);
    }

    public static async Task<IReadOnlyList<ShopServiceDto>> GetShopServicesAsync(int shopId, CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<IReadOnlyList<ShopServiceDto>>(http, $"services/shops/{shopId}/services", cancellationToken);
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

    public static async Task<ServiceRequestDto> SelectShopAsync(int requestId, SelectShopDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PutAsJsonAsync($"service-requests/{requestId}/select-shop", dto, cancellationToken);
        return await ReadAsync<ServiceRequestDto>(response, cancellationToken);
    }

    public static async Task AttachRequestMediaAsync(int requestId, UploadMediaDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync($"service-requests/{requestId}/media", dto, cancellationToken);
        await ReadAsync<object>(response, cancellationToken);
    }

    public static async Task<string> CreatePlaceholderFileAsync(UploadMediaDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("files/placeholder", dto, cancellationToken);
        var payload = await ReadAsync<PlaceholderFileDto>(response, cancellationToken);
        return payload.Url;
    }

    public static async Task<LiveLocationDto?> GetLatestRequestLocationAsync(int requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
            return await GetAsync<LiveLocationDto>(http, $"location/request/{requestId}/latest", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<MechanicProfileDto> GetMechanicProfileAsync(int mechanicId, CancellationToken cancellationToken = default)
    {
        using var http = ApiConfig.CreateHttpClient();
        return await GetAsync<MechanicProfileDto>(http, $"mechanics/{mechanicId}", cancellationToken);
    }

    public static async Task<PaymentDto> CreateCheckoutAsync(CreateCheckoutSessionDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("payments/create-checkout-session", dto, cancellationToken);
        return await ReadAsync<PaymentDto>(response, cancellationToken);
    }

    public static async Task<ReviewDto> SubmitReviewAsync(CreateReviewDto dto, CancellationToken cancellationToken = default)
    {
        using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
        using var response = await http.PostAsJsonAsync("reviews", dto, cancellationToken);
        return await ReadAsync<ReviewDto>(response, cancellationToken);
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

    private static string ContentTypeFor(FileResult file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            return file.ContentType;
        }

        return Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".3gp" => "video/3gpp",
            _ => "application/octet-stream"
        };
    }
}

internal sealed record CustomerMeDto(
    int ClientId,
    int UserId,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? ProfileImageUrl,
    bool EmailVerified,
    bool PhoneVerified,
    string AccountStatus,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? Sex,
    DateTime? Birthdate,
    string? ValidIdImageUrl,
    IReadOnlyList<CustomerAddressDto> Addresses,
    IReadOnlyList<MotorcycleDto> Motorcycles);

internal sealed record PlaceholderFileDto(string Url);

internal sealed record NotificationDto(
    int NotificationId,
    int UserId,
    string? NotificationType,
    string Title,
    string Message,
    string? DataJson,
    bool IsRead,
    DateTime CreatedAt);
