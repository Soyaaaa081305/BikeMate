using Microsoft.Extensions.Logging;

using BikeMate.Helpers;

namespace BikeMate
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("PublicSans.ttf", "PublicSans");
                    fonts.AddFont("Inter.ttf", "Inter");
                    fonts.AddFont("PTSansCaption-Regular.ttf", "PTSansCaption");
                    fonts.AddFont("PTSansCaption-Bold.ttf", "PTSansCaptionBold");
                });

            builder.Services.AddHttpClient("BikeMateApi", client =>
            {
                client.BaseAddress = new Uri(ApiConfig.BaseUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();

                if (ApiConfig.BaseUrl.Contains("10.0.2.2", StringComparison.OrdinalIgnoreCase) ||
                    ApiConfig.BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
