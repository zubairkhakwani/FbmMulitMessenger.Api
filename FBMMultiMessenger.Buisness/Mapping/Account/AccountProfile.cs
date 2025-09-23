using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


            CreateMap<ToggleAcountStatusModelResponse, ToggleAccountStatusHttpResponse>();
            CreateMap<BaseResponse<ToggleAcountStatusModelResponse>, BaseResponse<ToggleAccountStatusHttpResponse>>();

        }
    }
}
