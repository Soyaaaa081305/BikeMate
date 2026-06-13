using BikeMate.Core.Constants;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace BikeMate.Helpers;

public static class AppNavigation
{
    public const string ForceLoginPreferenceKey = "bikemate_force_login_after_logout";

    public static async Task NavigateByRoleAsync(string? role)
    {
        var route = role switch
        {
            AppRoles.Mechanic => "//MechanicDashboardPage",
            AppRoles.ShopAdmin => "//ShopDashboardPage",
            AppRoles.SystemAdmin => "//AdminDashboardPage",
            _ => "//CustomerHomePage"
        };

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();
#if ANDROID
            await Task.Delay(75);
#endif
            var shell = Shell.Current;
            if (shell is null)
            {
                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = new AppShell();
                    shell = Shell.Current;
                }
                else
                {
                    return;
                }
            }

            if (shell is not null)
            {
                await shell.GoToAsync(route);
            }
        });
    }

    public static async Task SignOutAsync()
    {
        SecureStorage.Default.Remove("access_token");
        SecureStorage.Default.Remove("primary_role");
        SecureStorage.Default.Remove("user_id");
        Preferences.Default.Set(ForceLoginPreferenceKey, true);

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("//MainPage");
            }
        });
    }

    public static string InferRoleFromEmail(string email)
    {
        email = email.Trim().ToLowerInvariant();
        if (email.Contains("mechanic")) return AppRoles.Mechanic;
        if (email.Contains("shop")) return AppRoles.ShopAdmin;
        if (email.Contains("admin")) return AppRoles.SystemAdmin;
        return AppRoles.Customer;
    }
}
