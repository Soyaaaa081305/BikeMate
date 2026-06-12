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
        EmailEntry.Text = "customer@bikemate.test";
        PasswordEntry.Text = "Password123!";
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        await SignInAsync();
    }

    private async void OnGoogleClicked(object? sender, EventArgs e)
    {
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;
        try
        {
            var auth = await GoogleSignInService.SignInAsync(AppRoles.Customer);
            await GoogleSignInService.StoreAuthAsync(auth);
            await AppNavigation.NavigateByRoleAsync(GoogleSignInService.PickPrimaryRole(auth.User.Roles));
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Google sign-in failed", ex.Message, "OK");
        }
        finally
        {
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private async Task SignInAsync()
    {
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;
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
                    navigatedAway = true;
                    await AppNavigation.NavigateByRoleAsync(role);
                    return;
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            await DisplayAlertAsync("Sign in failed", string.IsNullOrWhiteSpace(error) ? "Check your email and password." : error, "OK");
        }
        catch
        {
            navigatedAway = true;
            await AppNavigation.NavigateByRoleAsync(AppNavigation.InferRoleFromEmail(EmailEntry.Text ?? string.Empty));
        }
        finally
        {
            if (!navigatedAway)
            {
                BusyIndicator.IsRunning = false;
                BusyIndicator.IsVisible = false;
            }
        }
    }

    private static string PickPrimaryRole(IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(AppRoles.SystemAdmin)) return AppRoles.SystemAdmin;
        if (roles.Contains(AppRoles.ShopAdmin)) return AppRoles.ShopAdmin;
        if (roles.Contains(AppRoles.Mechanic)) return AppRoles.Mechanic;
        return AppRoles.Customer;
    }
}
