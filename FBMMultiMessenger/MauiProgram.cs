using FBMMultiMessenger.Notification;
using FBMMultiMessenger.Services;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using MudBlazor.Services;
using OneSignalSDK.DotNet;
using System.Reflection;


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

            // Load appsettings.json from root
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("FBMMultiMessenger.appsettings.json");

            if (stream != null)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                builder.Configuration.AddConfiguration(config);
            }

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddScoped<IBaseService, BaseService>();

            builder.Services.AddHttpClient<IAuthService, AuthService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            //builder.Services.AddHttpClient<IAccountService, AccountService>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<ITokenProvider, TokenProvider>();
            builder.Services.AddScoped<IChatMessagesService, ChatMessageService>();
            builder.Services.AddScoped<IExtensionService, ExtensionService>();
            builder.Services.AddScoped<ISubscriptionSerivce, SubscriptionService>();
            builder.Services.AddSingleton<BackButtonService>();
            builder.Services.AddSingleton<SignalRChatService>();
            builder.Services.AddScoped<OneSignalService>();

            builder.Services.AddHttpClient();
            builder.Services.AddMudServices();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var appId = builder.Configuration.GetValue<string>("OneSignal:AppId")!;
                OneSignal.Initialize(appId);
            }

            return builder.Build();
        }
    }
}
