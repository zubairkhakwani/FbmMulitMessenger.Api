using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;

namespace FBMMultiMessenger.Buisness.Service.IServices
{
    public interface IUserAccountService
    {
        Task<BaseResponse<EmailVerificationResponse>> ProcessEmailVerificationAsync(User user, CancellationToken cancellationToken);
    }
}
