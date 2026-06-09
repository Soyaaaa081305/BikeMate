using System.Net.Http.Json;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;

namespace BikeMate.Views.Auth;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;

        try
        {
            var dto = new RegisterRequestDto(
                FirstNameEntry.Text ?? string.Empty,
                LastNameEntry.Text ?? string.Empty,
                EmailEntry.Text ?? string.Empty,
                PasswordEntry.Text ?? string.Empty,
                ConfirmPasswordEntry.Text ?? string.Empty,
                PhoneEntry.Text,
                AppRoles.Customer);

            using var http = ApiConfig.CreateHttpClient();
            var response = await http.PostAsJsonAsync("auth/register", dto);

            if (response.IsSuccessStatusCode)
            {
                await Shell.Current.GoToAsync($"{nameof(OtpVerificationPage)}?email={Uri.EscapeDataString(dto.Email)}");
                return;
            }

            await DisplayAlertAsync("Registration failed", await response.Content.ReadAsStringAsync(), "OK");
        }
        catch
        {
            await DisplayAlertAsync("API offline", "Start the BikeMate API, then try registration again.", "OK");
        }
        finally
        {
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }
}
