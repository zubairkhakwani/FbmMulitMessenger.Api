using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class GetAllMyAccountsChatsModelRequest : IRequest<BaseResponse<GetAllMyAccountsChatsModelResponse>>
    {
        public int UserId { get; set; } // Current logged in user's id
    }

    public class GetAllMyAccountsChatsModelResponse
    {
        public List<GetMyChatsModelResponse> Chats { get; set; } = new List<GetMyChatsModelResponse>();
    }

    public class GetMyChatsModelResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string FbChatId { get; set; } = null!;
        public string FbLisFbListingTitle { get; set; } = null!;
        public string FbListingLocation { get; set; } = null!;
        public decimal FbListingPrice { get; set; }
        public string ImagePath { get; set; } = null!;
        public bool IsRead { get; set; }
        public int UnReadCount { get; set; }

        public DateTime StartedAt { get; set; }
    }


}
