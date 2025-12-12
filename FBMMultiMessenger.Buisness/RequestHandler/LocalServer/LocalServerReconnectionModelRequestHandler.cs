using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.LocalServer
{
    internal class LocalServerReconnectionModelRequestHandler : IRequestHandler<LocalServerReconnectionModelRequest, BaseResponse<LocalServerReconnectionModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;

        public LocalServerReconnectionModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }
        public async Task<BaseResponse<LocalServerReconnectionModelResponse>> Handle(LocalServerReconnectionModelRequest request, CancellationToken cancellationToken)
        {
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
