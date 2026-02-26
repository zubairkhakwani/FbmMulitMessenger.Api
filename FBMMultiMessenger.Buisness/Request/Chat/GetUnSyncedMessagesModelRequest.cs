using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class GetUnSyncedMessagesModelRequest : IRequest<BaseResponse<GetUnSyncedMessagesModelResponse>>
    {
        public DateTimeOffset? LastSyncedMessageAt { get; set; }
    }

    public class GetUnSyncedMessagesModelResponse
    {
        public bool HasActiveSubscription { get; set; }
        public List<SyncAccount> Accounts { get; set; }
        public List<SyncChat> Chats { get; set; }
        public DateTimeOffset LastSyncedAt { get; set; }
    }

    public class SyncChat
    {
        public int Id { get; set; }

        public int? AccountId { get; set; }
        public int UserId { get; set; }

        public string? FbUserId { get; set; }
        public string? FbAccountId { get; set; }
        public string? FBChatId { get; set; }
        public string? FbListingId { get; set; }
        public string? FbListingTitle { get; set; }
        public string? FbListingLocation { get; set; }
        public string? FBListingImage { get; set; }
        public string? UserProfileImage { get; set; }
        public string? OtherUserName { get; set; }
        public string? OtherUserId { get; set; }

        [Precision(18, 2)]
        public decimal? FbListingPrice { get; set; }

        public bool IsRead { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<SyncChatMessages> ChatMessages { get; set; } = new List<SyncChatMessages>();
    }

    public class SyncChatMessages
    {
        public int Id { get; set; }

        public int ChatId { get; set; }
        public string? FbMessageId { get; set; }

        public string? FbMessageReplyId { get; set; }

        public long? FBTimestamp { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool IsReceived { get; set; } // if true then the message was recevied other wise message was sent.

        public bool IsRead { get; set; }

        public bool IsSent { get; set; } //If true then means the message was succesfully sent otherwise inform user that message failed.
        public bool IsTextMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SyncAccount
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FbAccountId { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
