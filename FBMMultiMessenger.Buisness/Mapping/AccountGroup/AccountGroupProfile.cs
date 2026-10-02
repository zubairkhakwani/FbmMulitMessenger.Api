using AutoMapper;
using FBMMultiMessenger.Buisness.Request.AccountGroup;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Buisness.Mapping.AccountGroup
{
    public class AccountGroupProfile : Profile
    {
        public AccountGroupProfile()
        {
            CreateMap<UpsertAccountGroupHttpRequest, UpsertAccountGroupModelRequest>();
            CreateMap<AccountGroupModelResponse, AccountGroupHttpResponse>();
            CreateMap<GetMyAccountGroupsModelResponse, GetMyAccountGroupsHttpResponse>();
            CreateMap<BaseResponse<AccountGroupModelResponse>, BaseResponse<AccountGroupHttpResponse>>();
            CreateMap<BaseResponse<GetMyAccountGroupsModelResponse>, BaseResponse<GetMyAccountGroupsHttpResponse>>();
        }
    }
}
