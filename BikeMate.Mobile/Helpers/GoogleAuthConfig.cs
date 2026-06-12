namespace BikeMate.Helpers;

public static class GoogleAuthConfig
{
    public const string AndroidClientId = "1049211486363-l99ohnd6i2e4evptm2d4a39lqt0q58l4.apps.googleusercontent.com";
    public const string WebClientId = "1049211486363-kv5dfofjemltfhip5dif15lqqsfo4olb.apps.googleusercontent.com";
    public const string RedirectScheme = "com.googleusercontent.apps.1049211486363-l99ohnd6i2e4evptm2d4a39lqt0q58l4";
    public const string RedirectPath = "/oauth2redirect";
    public const string RedirectUri = RedirectScheme + ":/oauth2redirect";
}
