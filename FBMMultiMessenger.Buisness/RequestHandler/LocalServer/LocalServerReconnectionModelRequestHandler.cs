using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.LocalServer
{
    internal class LocalServerReconnectionModelRequestHandler : IRequestHandler<LocalServerReconnectionModelRequest, BaseResponse<LocalServerReconnectionModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISignalRService signalRService;

        public LocalServerReconnectionModelRequestHandler(ApplicationDbContext dbContext, ISignalRService signalRService)
        {
            this._dbContext=dbContext;
            this.signalRService = signalRService;
        }
        public async Task<BaseResponse<LocalServerReconnectionModelResponse>> Handle(LocalServerReconnectionModelRequest request, CancellationToken cancellationToken)
        {
            var dbAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId);
            if (dbAccount != null)
            {
                dbAccount.IsExtensionConnected = true;
                dbAccount.UpdatedAt = DateTime.UtcNow;
                _dbContext.Update(dbAccount);


                var signalrModel = new UserAccountSignalRModel
                {
                    AppId = request.UserId,
                    AccountsStatus = new List<AccountStatusSignalRModel> { new(){
                        AccountId = dbAccount.Id,
                        ConnectionStatus = AccountConnectionStatus.Online,
                        AuthStatus = AccountAuthStatus.LoggedIn,
                        IsConnected = true,
                        Reason = AccountReason.ConnectedWithExtension,
                    } }
                };

                await signalRService.NotifyAppAccountStatus(new List<UserAccountSignalRModel>() { signalrModel }, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return BaseResponse<LocalServerReconnectionModelResponse>.Success("", new LocalServerReconnectionModelResponse());


            var localServer = await _dbContext.LocalServers
                                        .FirstOrDefaultAsync(x => x.UniqueId == request.UniqueId, cancellationToken);

            if (localServer is null)
            {
                return BaseResponse<LocalServerReconnectionModelResponse>.Error("Local server not found.");
            }

            localServer.IsOnline = true;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<LocalServerReconnectionModelResponse>.Success("Local server marked as online.", new LocalServerReconnectionModelResponse());
        }
    }
}
