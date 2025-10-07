using FBMMultiMessenger.Notification;
using Blazored.LocalStorage;
using FBMMultiMessenger.AuthorizationPolicies.ActiveSubscriptionPolicy;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Services;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
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
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

            builder.Services.AddSingleton<SignalRChatService>();
            builder.Services.AddScoped<OneSignalService>();

            builder.Services.AddHttpClient();
            builder.Services.AddMudServices();
            builder.Services.AddBlazoredLocalStorage();

            builder.Services.AddAuthorizationCore(options =>
            {
                options.AddPolicy("ValidSubscription", policy =>
                {
                    policy.RequireAuthenticatedUser(); // This will fail if token doesn't exist
                    policy.AddRequirements(new ActiveSubscriptionRequirement());
                });
            });

            builder.Services.AddScoped<IAuthorizationHandler, ActiveSubscriptionRequirementHandler>();

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
