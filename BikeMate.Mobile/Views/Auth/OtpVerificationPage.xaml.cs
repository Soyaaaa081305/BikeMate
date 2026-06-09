using System.Net.Http.Json;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;

namespace BikeMate.Views.Auth;

[QueryProperty(nameof(Email), "email")]
public partial class OtpVerificationPage : ContentPage
{
    private string _email = string.Empty;

    public string Email
    {
        get => _email;
        set
        {
            _email = Uri.UnescapeDataString(value ?? string.Empty);
            EmailLabel.Text = $"Enter the OTP sent to {_email}.";
        }
    }

    public OtpVerificationPage()
    {
        InitializeComponent();
    }

    private async void OnVerifyClicked(object? sender, EventArgs e)
    {
        await RunOtpRequestAsync("auth/verify-otp", new VerifyOtpRequestDto(Email, OtpEntry.Text ?? string.Empty, "email_verification"), true);
    }

    private async void OnResendClicked(object? sender, EventArgs e)
    {
        await RunOtpRequestAsync("auth/resend-otp", new ResendOtpRequestDto(Email, "email_verification"), false);
    }

    private async Task RunOtpRequestAsync<T>(string route, T dto, bool goToLogin)
    {
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;

        try
        {
                using var http = ApiConfig.CreateHttpClient();
                var response = await http.PostAsJsonAsync(route, dto);
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Success", goToLogin ? "Email verified. You can now sign in." : "OTP resent.", "OK");
                if (goToLogin)
                {
                    await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
                }
                return;
            }

            await DisplayAlertAsync("OTP failed", await response.Content.ReadAsStringAsync(), "OK");
        }
        catch
        {
            await DisplayAlertAsync("API offline", "Start the BikeMate API, then try again.", "OK");
        }
        finally
        {
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }
}
