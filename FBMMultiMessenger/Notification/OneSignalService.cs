using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;
using OneSignalSDK.DotNet;
using OneSignalSDK.DotNet.Core.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Notification
{
    internal class OneSignalService
    {
        public NavigationManager Navigation { get; set; }

        public OneSignalService(NavigationManager navigation)
        {
            this.Navigation = navigation;
        }

        public void Login(string userId)
        {
            OneSignal.Login(userId);
        }

        public async Task AskNotificationPermissionAsync()
        {

            await OneSignal.Notifications.RequestPermissionAsync(true);
        }

        public void OnNotificationClicked()
        {
            OneSignal.Notifications.Clicked -= HandleNotificationClicked;

            OneSignal.Notifications.Clicked += HandleNotificationClicked;
        }

        public void HandleNotificationClicked(object sender, NotificationClickedEventArgs e)
        {
            var data = e.Notification.AdditionalData;
            var hasFbChatIdKey = data.TryGetValue("chatId", out var fbChatIdObj);
            var hasSubscriptionExpiredKey = data.TryGetValue("isSubscriptionExpired", out var subscriptionExpiredObj);
            var messageKey = data.TryGetValue("message", out var message);

            if (data != null && hasFbChatIdKey && hasSubscriptionExpiredKey)
            {
                string fbChatId = fbChatIdObj!.ToString()!;
                bool isParsed = bool.TryParse(subscriptionExpiredObj!.ToString(), out bool isSubscriptionExpired);

                // Navigate to chat if subscription is not expired otherwise navigate to subscription page.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!isSubscriptionExpired)
                    {
                        Navigation.NavigateTo($"/chat?isNotification=true&fbChatId={fbChatId}");
                    }
                    else
                    {
                        Navigation.NavigateTo($"/packages?isExpired={isSubscriptionExpired}&message={message!.ToString()}");
                    }
                });
            }
        }

    }
}
