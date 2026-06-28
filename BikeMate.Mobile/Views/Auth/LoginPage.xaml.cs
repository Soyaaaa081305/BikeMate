using System.Net.Http.Json;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;

namespace BikeMate.Views.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        AppVisualPolish.Apply((View)Content);
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        await SignInAsync();
    }

    private async void OnGoogleClicked(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            var auth = await GoogleSignInService.SignInAsync(AppRoles.Customer);
            await GoogleSignInService.StoreAuthAsync(auth);
            await AppNavigation.NavigateByRoleAsync(GoogleSignInService.PickPrimaryRole(auth.User.Roles));
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync("Google sign-in cancelled", "Google sign-in was cancelled. You can try again anytime.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google sign-in failed: {ex}");
            await DisplayAlertAsync("Google sign-in unavailable", ex.Message, "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private async void OnForgotPasswordClicked(object? sender, EventArgs e)
    {
        var email = Uri.EscapeDataString(EmailEntry.Text?.Trim() ?? string.Empty);
        await Shell.Current.GoToAsync($"{nameof(PasswordResetPage)}?email={email}");
    }

    private async void OnPasswordCompleted(object? sender, EventArgs e)
    {
        await SignInAsync();
    }

    private async Task SignInAsync()
    {
        SetBusy(true);
        var navigatedAway = false;

        try
        {
            using var http = ApiConfig.CreateHttpClient();
            var response = await http.PostAsJsonAsync("auth/login", new LoginRequestDto(EmailEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty));

            if (response.IsSuccessStatusCode)
            {
                var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (auth is not null)
                {
                    await SecureStorage.Default.SetAsync("access_token", auth.AccessToken);
                    var role = PickPrimaryRole(auth.User.Roles);
                    await SecureStorage.Default.SetAsync("primary_role", role);
                    await SecureStorage.Default.SetAsync("user_id", auth.User.UserId.ToString());
                    navigatedAway = true;
                    await AppNavigation.NavigateByRoleAsync(role);
                    return;
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            await DisplayAlertAsync("Sign in failed", string.IsNullOrWhiteSpace(error) ? "Check your email and password." : error, "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sign in failed: {ex}");
            await DisplayAlertAsync("Sign in unavailable", "Could not reach the BikeMate API. Start the API and try again.", "OK");
        }
        finally
        {
            if (!navigatedAway)
            {
                SetBusy(false);
            }
        }
    }

    private void SetBusy(bool value)
    {
        BusyIndicator.IsVisible = value;
        BusyIndicator.IsRunning = value;
        SignInButton.IsEnabled = !value;
        GoogleButton.IsEnabled = !value;
    }

    private static string PickPrimaryRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(AppRoles.SystemAdmin)) return AppRoles.SystemAdmin;
        if (roles.Contains(AppRoles.ShopAdmin)) return AppRoles.ShopAdmin;
        if (roles.Contains(AppRoles.Mechanic)) return AppRoles.Mechanic;
        return AppRoles.Customer;
    }
}
