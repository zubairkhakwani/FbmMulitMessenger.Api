using Azure.Core;
using FBMMultiMessenger.Buisness.OneSignal;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class ReceiveChatModeRequestHandler : IRequestHandler<ReceiveChatModelRequest, BaseResponse<ReceiveChatModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly OneSignalNotificationService _oneSignalNotificationService;

        public ReceiveChatModeRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext, OneSignalNotificationService oneSignalNotificationService)
        {
            this._dbContext=dbContext;
            this._hubContext = hubContext;
            this._oneSignalNotificationService=oneSignalNotificationService;
        }
        public async Task<BaseResponse<ReceiveChatModelResponse>> Handle(ReceiveChatModelRequest request, CancellationToken cancellationToken)
        {
            var chat = await _dbContext.Chats
                                       .Include(u => u.User)
                                       .ThenInclude(s => s.Subscriptions)
                                       .FirstOrDefaultAsync(x => x.FBChatId == request.FbChatId, cancellationToken);

            var chatReference = chat;

            if (chat is null)
            {
                var newChat = new Chat()
                {
                    //AccountId = request.AccountId,
                    UserId = request.UserId,

                    FBChatId = request.FbChatId,
                    FbAccountId = request.FbAccountId,
                    FbListingId = request.FbListingId,
                    FbListingTitle = string.IsNullOrWhiteSpace(request.FbListingTitle) ? "No title" : request.FbListingTitle,
                    FbListingLocation = string.IsNullOrWhiteSpace(request.FbListingLocation) ? "No location" : request.FbListingLocation,
                    FbListingPrice = request.FbListingPrice,

                    ImagePath = null,
                    IsRead = false,
                    StartedAt = DateTime.Now,
                };

                await _dbContext.AddAsync(newChat, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                chatReference = newChat;
            }

            var dbMessage = string.Empty;

            if (request.IsImageMessage)
            {
                dbMessage = JsonSerializer.Serialize(request.Messages);
            }
            else if (request.IsTextMessage)
            {
                dbMessage = request.Messages.FirstOrDefault()?.Trim();
            }

            var newChatMessage = new ChatMessages()
            {
                Message = dbMessage.Trim(),
                ChatId = chatReference!.Id,
                IsReceived = true,
                IsRead = false,
                IsTextMessage = request.IsTextMessage,
                IsVideoMessage = request.IsVideoMessage,
                IsImageMessage = request.IsImageMessage,
                IsAudioMessage = request.IsAudioMessage,

                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.ChatMessages.AddAsync(newChatMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var subscriptions = chatReference?.User?.Subscriptions;
            var today = DateTime.Now;
            var endDate = subscriptions?.LastOrDefault()?.ExpiredAt;

            if (subscriptions is null || today >= endDate)
            {
                var responseMessage = string.Empty;
                if (subscriptions is null)
                {
                    responseMessage = "user does not have any active subscription. No notification was sent and the UI was not updated. ";
                }
                else
                {
                    await SendMobileNotificationAsync(request, isSubscriptionExpired: true);
                    responseMessage = "user's subscription has expired";
                }
                return BaseResponse<ReceiveChatModelResponse>.Success($"Message received, but the {responseMessage}. ", new ReceiveChatModelResponse());
            }

            // will send notification to andriod level via one signal.
            await SendMobileNotificationAsync(request);


            //Inform the client that the message has been received via signalR.
            var receivedChat = new ReceiveChatHttpResponse()
            {
                Message = dbMessage,
                ChatId = chat.Id,
                FbChatId = request.FbChatId,
                FbAccountId = request.FbAccountId,
                FbListingId = request.FbListingId,
                FbListingTitle = chat.FbListingTitle!,
                FbListingLocation = chat.FbListingLocation!,
                FbListingPrice = chat.FbListingPrice!.Value,
                IsTextMessage = request.IsTextMessage,
                IsVideoMessage =request.IsVideoMessage,
                IsImageMessage = request.IsImageMessage,
                IsAudioMessage = request.IsAudioMessage,
                StartedAt = DateTime.UtcNow,
            };

            //TODO
            await _hubContext.Clients.Group("User123")
                .SendAsync("ReceiveMessage", receivedChat, cancellationToken);


            return BaseResponse<ReceiveChatModelResponse>.Success($"Message has been received successfully", new ReceiveChatModelResponse());
        }
        private async Task SendMobileNotificationAsync(ReceiveChatModelRequest request, bool isSubscriptionExpired = false)
        {
            string message = string.Empty;

            if (!isSubscriptionExpired)
            {
                var count = request.Messages.Count;

                string receivedMessage = request switch
                {
                    { IsImageMessage: true } => $"You have received {count} images",
                    { IsVideoMessage: true } => $"You have received {count} videos",
                    { IsAudioMessage: true } => $"You have received {count} videos",
                    _ => request.Messages.FirstOrDefault()!
                };
                message = receivedMessage;
            }
            else
            {
                message = "New messages waiting! Renew your subscription to view them.";
                request.FbChatId = string.Empty;
            }


            await _oneSignalNotificationService.SendMessageNotification(
                userId: request.UserId.ToString(),
                message: message,
                senderName: "FBM MULTI MESSENGER",
                chatId: request.FbChatId,
                isSubscriptionExpired: isSubscriptionExpired
            );
        }
    }
}