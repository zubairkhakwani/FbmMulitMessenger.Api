using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Extension
{
    internal class GetExtensionVersionRequestHandler(ApplicationDbContext dbContext) : IRequestHandler<GetExtensionVersionRequest, BaseResponse<GetExtensionVersionModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task<BaseResponse<GetExtensionVersionModelResponse>> Handle(GetExtensionVersionRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var settings = await _dbContext.Settings.FirstOrDefaultAsync(cancellationToken);

                var response = new GetExtensionVersionModelResponse()
                {
                    Version = settings?.Extension_Version ?? string.Empty
                };

                return BaseResponse<GetExtensionVersionModelResponse>.Success("Successfully loaded extension version", response);
            }
            catch (Exception ex)
            {
                return BaseResponse<GetExtensionVersionModelResponse>.Error("Something went wrong while fetching extension version.");
            }
        }
    }
}
