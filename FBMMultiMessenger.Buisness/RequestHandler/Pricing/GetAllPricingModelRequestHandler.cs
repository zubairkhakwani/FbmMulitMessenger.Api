using FBMMultiMessenger.Buisness.Request.Pricing;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Pricing
{
    internal class GetAllPricingModelRequestHandler : IRequestHandler<GetAllPricingModelRequest, BaseResponse<GetAllPricingModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;

        public GetAllPricingModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }
        public async Task<BaseResponse<GetAllPricingModelResponse>> Handle(GetAllPricingModelRequest request, CancellationToken cancellationToken)
        {
            var pricingTiers = await _dbContext
                                         .PricingTiers
                                         .OrderBy(x => x.UptoAccounts)
                                         .ToListAsync(cancellationToken);

            var response = new GetAllPricingModelResponse()
            {
                AccountDetails =new List<AccountDetailsModelResponse>
                {
                    new AccountDetailsModelResponse
                    {
                        BankName = "Habib Bank Limited (HBL)",
                        Title = "M ZUBAIR KHAN",
                        AccountNo = "02317901039803",
                        IBAN = " PK70HABB0002317901039803"
                    },
                    new AccountDetailsModelResponse
                    {
                        BankName = "Easypaisa",
                        Title = "Muhammad zubair khan",
                        AccountNo = "03476774413",
                    },
                     new AccountDetailsModelResponse
                    {
                        BankName = "Jazz cash",
                        Title = "Muhammad zubair khan",
                        AccountNo = "03185924729",
                    }
                },
                PricingTiers = pricingTiers.Select(x => new PricingTierModelResponse()
                {
                    Id = x.Id,
                    UptoAccounts = x.UptoAccounts,
                    MonthlyPrice = x.MonthlyPrice,
                    SemiAnnualPrice = x.SemiAnnualPrice,
                    AnnualPrice = x.AnnualPrice
                }).ToList()
            };
            return BaseResponse<GetAllPricingModelResponse>.Success("Operation performed successully", response);
        }
    }
}
