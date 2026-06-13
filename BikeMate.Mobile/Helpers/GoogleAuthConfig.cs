namespace BikeMate.Helpers;

/// <summary>
/// Google OAuth configuration.
/// Replace placeholder values with your real client IDs in a local (git-ignored)
/// config file or environment variables before building for production.
/// DO NOT commit real client IDs to source control.
/// </summary>
public static class GoogleAuthConfig
{
    // These must be const for Android IntentFilter attributes.
    // Replace at build time via .csproj DefineConstants or local overrides.
    public const string RedirectScheme = "com.googleusercontent.apps.YOUR_ANDROID_CLIENT_ID";
    public const string RedirectPath = "/oauth2redirect";
    public const string RedirectUri = RedirectScheme + ":/oauth2redirect";
    public const string ApiCallbackUri = "bikemate://auth/google";

    // Runtime values — override via environment variables or MauiProgram configuration.
    public static readonly string AndroidClientId =
        Environment.GetEnvironmentVariable("GOOGLE_ANDROID_CLIENT_ID") ?? "YOUR_GOOGLE_ANDROID_CLIENT_ID";

    public static readonly string WebClientId =
        Environment.GetEnvironmentVariable("GOOGLE_WEB_CLIENT_ID") ?? "YOUR_GOOGLE_WEB_CLIENT_ID";
}
