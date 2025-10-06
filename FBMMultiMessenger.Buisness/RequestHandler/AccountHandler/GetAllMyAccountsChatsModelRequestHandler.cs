using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class GetAllMyAccountsChatsModelRequestHandler : IRequestHandler<GetAllMyAccountsChatsModelRequest, BaseResponse<GetAllMyAccountsChatsModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;

        public GetAllMyAccountsChatsModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext = dbContext;
        }
        public async Task<BaseResponse<GetAllMyAccountsChatsModelResponse>> Handle(GetAllMyAccountsChatsModelRequest request, CancellationToken cancellationToken)
        {
            var chats = await _dbContext.Chats
                                        .Include(cm => cm.ChatMessages)
                                        .Where(u => u.UserId == request.UserId).ToListAsync(cancellationToken);

            var formattedChats = chats.Select(x => new GetMyChatsModelResponse()
            {
                Id = x.Id,
                FbChatId = x.FBChatId!,
                FbLisFbListingTitle = x.FbListingTitle!,
                FbListingPrice = x.FbListingPrice!.Value,
                FbListingLocation = x.FbListingLocation!,
                IsRead = x.IsRead,
                UnReadCount = x.ChatMessages.Where(x => !x.IsRead).Count(),
                StartedAt = x.StartedAt,
            }).OrderByDescending(x => x.StartedAt).ToList();


            var response = new GetAllMyAccountsChatsModelResponse()
            {
                Chats = formattedChats
            };

            return BaseResponse<GetAllMyAccountsChatsModelResponse>.Success("Operation performed successfully", response);
        }
    }
}
