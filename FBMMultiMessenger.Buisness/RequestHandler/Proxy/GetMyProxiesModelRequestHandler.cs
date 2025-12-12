using FBMMultiMessenger.Buisness.Request.Proxy;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Proxy
{
    internal class GetMyProxiesModelRequestHandler : IRequestHandler<GetMyProxiesModelRequest, BaseResponse<PageableResponse<GetMyProxiesModelResponse>>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetMyProxiesModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<PageableResponse<GetMyProxiesModelResponse>>> Handle(GetMyProxiesModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)
            {
                return BaseResponse<PageableResponse<GetMyProxiesModelResponse>>.Error("Invalid request, please login again to continue");
            }

            var proxies = await _dbContext.Proxies
                                          .Skip((request.PageNo - 1) * request.PageSize)
                                          .Take(request.PageSize)
                                          .Where(u => u.UserId == currentUser.Id)
                                          .ToListAsync(cancellationToken);

            var records = proxies.Select(x => new GetMyProxiesModelResponse()
            {
                Id = x.Id,
                Ip_Port = x.Ip_Port,
                Name = x.Name,
                Password = x.Password,
                CreatedAt = DateTime.UtcNow,

            }).ToList();

            var count = proxies.Count;
            var pageableResponse = new PageableResponse<GetMyProxiesModelResponse>(records, request.PageNo, request.PageSize, count, count/request.PageSize);

            return BaseResponse<PageableResponse<GetMyProxiesModelResponse>>.Success("Operation performed successfully", pageableResponse);
        }
    }
}
