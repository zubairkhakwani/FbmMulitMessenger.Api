using FBMMultiMessenger.Data.Database.DbModels;

namespace FBMMultiMessenger.Buisness.Service.IServices
{
    public interface IVerificationCodeService
    {
        string GenerateOTP();
        bool HasValidOtp(User user, bool isEmailVerificationCode = false);
    }
}
