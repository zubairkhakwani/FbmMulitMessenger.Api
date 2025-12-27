using AutoMapper;
using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Contracts.Contracts.Account;
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

            //Get My Accounts Mappings Start
            CreateMap<GetMyAccountsHttpRequest, GetMyAccountsModelRequest>();
            CreateMap<UserAccountsOverviewModelResponse, UserAccountsOverviewHttpResponse>();
            CreateMap<BaseResponse<UserAccountsOverviewModelResponse>, BaseResponse<UserAccountsOverviewHttpResponse>>();
            CreateMap<UserAccountsModelResponse, UserAccountsHttpResponse>();
            CreateMap<PageableResponse<UserAccountsModelResponse>, PageableResponse<UserAccountsHttpResponse>>();
            CreateMap<BaseResponse<List<UserAccountsModelResponse>>, BaseResponse<List<UserAccountsHttpResponse>>>();
            CreateMap<BaseResponse<PageableResponse<UserAccountsModelResponse>>, BaseResponse<PageableResponse<UserAccountsHttpResponse>>>();
            CreateMap<AccountProxyModelResponse, AccountProxyHttpResponse>();
            //Get My Accounts Mappings End

            CreateMap<ToggleAcountStatusModelResponse, RemoveAccountHttpResponse>();
            CreateMap<BaseResponse<ToggleAcountStatusModelResponse>, BaseResponse<RemoveAccountHttpResponse>>();

            //Get All My Accounts Chats Mappings Start
            CreateMap<GetAllMyAccountsChatsModelResponse, GetAllMyAccountsChatsHttpResponse>();
            CreateMap<GetMyChatsModelResponse, GetMyChatsHttpResponse>();
            CreateMap<GetMyChatAccountModelResponse, GetMyChatAccountHttpResponse>();
            CreateMap<GetMyChatMessagesModelResonse, GetMyChatMessagesHttpResonse>();
            CreateMap<BaseResponse<GetAllMyAccountsChatsModelResponse>, BaseResponse<GetAllMyAccountsChatsHttpResponse>>();
            //Get All My Accounts Chats Mappings End

            CreateMap<GetChatMessagesModelResponse, GetAllMyAccountsChatsHttpResponse>();
            CreateMap<BaseResponse<List<GetChatMessagesModelResponse>>, BaseResponse<List<GetAllMyAccountsChatsHttpResponse>>>();

            CreateMap<ImportAccountsHttpRequest, ImportAccounts>();


            CreateMap<GetEncExtensionContentModelResponse, UserAccountsHttpResponse>();
            CreateMap<BaseResponse<GetEncExtensionContentModelResponse>, BaseResponse<UserAccountsHttpResponse>>();


            CreateMap<EmailVerificationResponse, UpsertAccountModelResponse>();
            CreateMap<BaseResponse<EmailVerificationResponse>, BaseResponse<UpsertAccountModelResponse>>();




            CreateMap<UpdateAccountStatusHttpRequest, UpdateAccountStatusModelRequest>();
            CreateMap<AccountUpdateHttpOperation, AccountUpdateModelOperation>();

            CreateMap<UpdateAccountStatusModelResponse, UpdateAccountStatusHttpResponse>();
            CreateMap<BaseResponse<UpdateAccountStatusModelResponse>, BaseResponse<UpdateAccountStatusHttpResponse>>();
        }
    }
}
