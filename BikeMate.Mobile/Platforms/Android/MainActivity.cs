using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using BikeMate.Services;

namespace BikeMate;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTop)]
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
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleDeepLink(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleDeepLink(intent);
    }

    private static void HandleDeepLink(Intent? intent)
    {
        if (PaymentReturnService.CaptureReturn(intent?.DataString))
        {
            intent?.SetData(null);
        }
    }
}
