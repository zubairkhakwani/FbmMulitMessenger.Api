using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Buisness.Mapping.Account
{
    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            CreateMap<UpsertAccountHttpRequest, UpsertAccountModelRequest>();

            CreateMap<UpsertAccountModelResponse, UpsertAccountHttpResponse>();

            CreateMap<BaseResponse<UpsertAccountModelResponse>, BaseResponse<UpsertAccountHttpResponse>>();


            CreateMap<GetMyAccountsModelResponse, GetMyAccountsHttpResponse>();
            CreateMap<BaseResponse<List<GetMyAccountsModelResponse>>, BaseResponse<List<GetMyAccountsHttpResponse>>>();


            CreateMap<ToggleAcountStatusModelResponse, RemoveAccountHttpResponse>();
            CreateMap<BaseResponse<ToggleAcountStatusModelResponse>, BaseResponse<RemoveAccountHttpResponse>>();


            CreateMap<GetAllMyAccountsChatsModelResponse, GetAllMyAccountsChatsHttpResponse>();
            CreateMap<GetMyChatsModelResponse, GetMyChatsHttpResponse>();

            CreateMap<BaseResponse<GetAllMyAccountsChatsModelResponse>, BaseResponse<GetAllMyAccountsChatsHttpResponse>>();

            CreateMap<GetChatMessagesModelResponse, GetAllMyAccountsChatsHttpResponse>();
            CreateMap<BaseResponse<List<GetChatMessagesModelResponse>>, BaseResponse<List<GetAllMyAccountsChatsHttpResponse>>>();

            CreateMap<ImportAccountsHttpRequest, ImportAccounts>();


            CreateMap<GetEncExtensionContentModelResponse, GetMyAccountsHttpResponse>();
            CreateMap<BaseResponse<GetEncExtensionContentModelResponse>, BaseResponse<GetMyAccountsHttpResponse>>();
        }
    }
}
