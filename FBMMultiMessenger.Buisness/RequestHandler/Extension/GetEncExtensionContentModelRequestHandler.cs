using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Extension
{
    internal class GetEncExtensionContentModelRequestHandler(AesEncryptionHelper aesEncryptionHelper, ApplicationDbContext dbContext, ExtensionContentCache cache) : IRequestHandler<GetEncExtensionContentModelRequest, BaseResponse<GetEncExtensionContentModelResponse>>
    {
        private readonly AesEncryptionHelper _aesEncryptionHelper = aesEncryptionHelper;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly ExtensionContentCache _cache = cache;

        public async Task<BaseResponse<GetEncExtensionContentModelResponse>> Handle(GetEncExtensionContentModelRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var settings = await _dbContext.Settings.FirstOrDefaultAsync(cancellationToken);

                string encryptedExtensionFiles = await _cache.GetAsync(settings?.Extension_Version, _aesEncryptionHelper);

                var response = new GetEncExtensionContentModelResponse()
                {
                    Css = encryptedExtensionFiles
                };

                return BaseResponse<GetEncExtensionContentModelResponse>.Success("Successfully loaded bootstrap css", response);
            }
            catch (Exception ex)
            {
                return BaseResponse<GetEncExtensionContentModelResponse>.Error("Something went wrong while fetching bootstrap css.");
            }
        }
    }
}
