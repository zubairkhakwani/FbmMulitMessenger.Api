using AutoMapper;
using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.LocalServer;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Buisness.Mapping.Account
{
    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            CreateMap<UpsertAccountHttpRequest, UpsertAccountModelRequest>();

            CreateMap<UpsertAccountModelResponse, UpsertAccountHttpResponse>();

            CreateMap<BaseResponse<UpsertAccountModelResponse>, BaseResponse<UpsertAccountHttpResponse>>();

            CreateMap<AccountProxyModelResponse, AccountProxyHttpResponse>();
            CreateMap<GetMyAccountsHttpRequest, GetMyAccountsModelRequest>();
            CreateMap<GetMyAccountsModelResponse, GetMyAccountsHttpResponse>();

            CreateMap<PageableResponse<GetMyAccountsModelResponse>, PageableResponse<GetMyAccountsHttpResponse>>();

            CreateMap<BaseResponse<List<GetMyAccountsModelResponse>>, BaseResponse<List<GetMyAccountsHttpResponse>>>();
            CreateMap<BaseResponse<PageableResponse<GetMyAccountsModelResponse>>, BaseResponse<PageableResponse<GetMyAccountsHttpResponse>>>();


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


            CreateMap<EmailVerificationResponse, UpsertAccountModelResponse>();
            CreateMap<BaseResponse<EmailVerificationResponse>, BaseResponse<UpsertAccountModelResponse>>();




            CreateMap<UpdateAccountStatusHttpRequest, UpdateAccountStatusModelRequest>();
            CreateMap<UpdateAccountStatusModelResponse, UpdateAccountStatusHttpResponse>();
            CreateMap<BaseResponse<UpdateAccountStatusModelResponse>, BaseResponse<UpdateAccountStatusHttpResponse>>();
        }
    }
}
