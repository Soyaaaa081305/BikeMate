using BikeMate.Core.Constants;

namespace BikeMate.Helpers;

public static class AppNavigation
{
    public static Task NavigateByRoleAsync(string? role)
    {
        Page page = role switch
        {
            AppRoles.Mechanic => new MechanicShell(),
            AppRoles.ShopAdmin => new ShopAdminShell(),
            AppRoles.SystemAdmin => new AdminShell(),
            _ => new CustomerShell()
        };

        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = page;
        }

        return Task.CompletedTask;
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
