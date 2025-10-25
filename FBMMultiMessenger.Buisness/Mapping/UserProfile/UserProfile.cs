using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Profile;
using FBMMultiMessenger.Contracts.Contracts.Profile;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Buisness.Mapping.UserProfile
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<EditProfileHttpRequest, EditProfileModelRequest>();
            CreateMap<ChangePasswordHttpRequest, ChangePasswordModelRequest>();

            CreateMap<GetMyProfileModelResponse, GetMyProfileHttpResponse>();
            CreateMap<BaseResponse<GetMyProfileModelResponse>, BaseResponse<GetMyProfileHttpResponse>>();
        }
    }
}
