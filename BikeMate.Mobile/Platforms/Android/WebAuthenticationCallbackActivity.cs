using Android.App;
using Android.Content;
using Android.Content.PM;
using BikeMate.Helpers;
using Microsoft.Maui.Authentication;

namespace BikeMate;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = GoogleAuthConfig.RedirectScheme,
    DataPath = GoogleAuthConfig.RedirectPath)]
public sealed class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}
