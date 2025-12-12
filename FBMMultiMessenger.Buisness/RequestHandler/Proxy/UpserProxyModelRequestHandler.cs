using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Proxy;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Proxy
{
    internal class UpserProxyModelRequestHandler : IRequestHandler<UpsertProxyModelRequest, BaseResponse<UpsertProxyModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public UpserProxyModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<UpsertProxyModelResponse>> Handle(UpsertProxyModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)
            {
                return BaseResponse<UpsertProxyModelResponse>.Error("Invalid request, Please login again to continue");
            }

            var proxyResponse = await ProxyHelper.GetProxyDetail(request.Ip_Port, request.Name, request.Password);

            if (proxyResponse is null)
            {
                return BaseResponse<UpsertProxyModelResponse>.Error("Invalid Proxy");
            }


            request.CurrentUserId = currentUser.Id;

            if (request.ProxyId is null)
            {
                return await AddRequestAsync(request, cancellationToken);
            }

            return await UpdateRequestAsync(request, cancellationToken);

        }

        private async Task<BaseResponse<UpsertProxyModelResponse>> AddRequestAsync(UpsertProxyModelRequest request, CancellationToken cancellationToken)
        {
            bool ipExists = await _dbContext.Proxies.AnyAsync(x => x.Ip_Port == request.Ip_Port, cancellationToken);

            if (ipExists)
            {
                return BaseResponse<UpsertProxyModelResponse>.Error("IP Port already exists.");
            }

            var newProxy = new Data.Database.DbModels.Proxy()
            {
                Ip_Port = request.Ip_Port,
                Name = request.Name,
                Password = request.Password,
                CreatedAt = DateTime.UtcNow,
                UserId = request.CurrentUserId,
                IsActive = true
            };

            await _dbContext.Proxies.AddAsync(newProxy, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<UpsertProxyModelResponse>.Success("Proxy added successfully", new UpsertProxyModelResponse());
        }

        private async Task<BaseResponse<UpsertProxyModelResponse>> UpdateRequestAsync(UpsertProxyModelRequest request, CancellationToken cancellationToken)
        {
            var proxy = await _dbContext.Proxies
                                        .FirstOrDefaultAsync(x => x.Id == request.ProxyId, cancellationToken);

            if (proxy is null)
            {
                return BaseResponse<UpsertProxyModelResponse>.Error("Proxy does not exist");

            }

            proxy.Ip_Port = request.Ip_Port;
            proxy.Name = request.Name;
            proxy.Password = request.Password;
            proxy.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<UpsertProxyModelResponse>.Success("Proxy updated successfully", new UpsertProxyModelResponse());
        }
    }
}
