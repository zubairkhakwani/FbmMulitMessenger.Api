using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    public class UpdateAccountStatusFromExtensionRequestHandler : IRequestHandler<UpdateAccountStatusFromExtensionRequest, BaseResponse<UpdateAccountStatusFromExtensionResponse>>
    {
        private readonly ApplicationDbContext dbContext;
        private readonly CurrentUserService currentUserService;
        private readonly ISignalRService signalRService;

        public UpdateAccountStatusFromExtensionRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, ISignalRService signalRService)
        {
            this.dbContext = dbContext;
            this.currentUserService = currentUserService;
            this.signalRService = signalRService;
        }

        public async Task<BaseResponse<UpdateAccountStatusFromExtensionResponse>> Handle(UpdateAccountStatusFromExtensionRequest request, CancellationToken cancellationToken)
        {
            var currentUser = currentUserService.GetCurrentUser();

            var dbAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == currentUser.Id);

            if (dbAccount != null)
            {
                dbAccount.UpdatedAt = DateTime.UtcNow;
                dbAccount.AuthStatus = request.AccountAuthStatus;
                dbAccount.ConnectionStatus = request.AccountConnectionStatus;
                dbAccount.Reason = request.Reason;
                dbAccount.IsExtensionConnected = request.IsLoggedIn;

                var signalrModel = new UserAccountSignalRModel
                {
                    AppId = currentUser.Id,
                    AccountsStatus = new List<AccountStatusSignalRModel> { new(){
                        AccountId = dbAccount.Id,
                        ConnectionStatus = request.AccountConnectionStatus,
                        AuthStatus = request.AccountAuthStatus,
                        IsConnected = request.IsLoggedIn,
                        Reason = request.Reason,
                        
                    } }
                };

                await signalRService.NotifyAppAccountStatus(new List<UserAccountSignalRModel>() { signalrModel }, cancellationToken);

                await dbContext.SaveChangesAsync();
            }

            return BaseResponse<UpdateAccountStatusFromExtensionResponse>.Success("", new());
        }
    }
}
