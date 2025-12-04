using FBMMultiMessenger.Buisness.Request.Pricing;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Pricing
{
    internal class GetAllPricingModelRequestHandler : IRequestHandler<GetAllPricingModelRequest, BaseResponse<List<GetAllPricingModelResponse>>>
    {
        private readonly ApplicationDbContext _dbContext;

        public GetAllPricingModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }
        public async Task<BaseResponse<List<GetAllPricingModelResponse>>> Handle(GetAllPricingModelRequest request, CancellationToken cancellationToken)
        {
            var pricingTiers = await _dbContext
                                         .PricingTiers
                                         .OrderBy(x => x.MinAccounts)
                                         .ToListAsync(cancellationToken);

            var response = pricingTiers.Select(x => new GetAllPricingModelResponse()
            {
                MinAccounts = x.MinAccounts,
                MaxAccounts = x.MaxAccounts,
                PricePerAccount = x.PricePerAccount

            }).ToList();

            return BaseResponse<List<GetAllPricingModelResponse>>.Success("Operation performed successully", response);
        }
    }
}
