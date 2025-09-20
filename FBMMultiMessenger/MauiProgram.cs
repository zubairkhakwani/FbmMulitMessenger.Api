using FBMMultiMessenger.Services;
using FBMMultiMessenger.Services.IServices;
using Microsoft.Extensions.Logging;

namespace FBMMultiMessenger
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
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddScoped<IBaseService, BaseService>();

            builder.Services.AddHttpClient<IAuthService, AuthService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services.AddHttpClient();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
