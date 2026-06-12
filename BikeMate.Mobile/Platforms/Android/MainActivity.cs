using Android.App;
using Android.Content.PM;
using Android.Content;

namespace BikeMate;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
              Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
              DataScheme = "bikemate",
              DataHost = "auth")]
[IntentFilter(new[] { Intent.ActionView },
              Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
              DataScheme = "bikemate",
              DataHost = "payment-success")]
[IntentFilter(new[] { Intent.ActionView },
              Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
              DataScheme = "bikemate",
              DataHost = "payment-cancelled")]
public class MainActivity : MauiAppCompatActivity
{
}
