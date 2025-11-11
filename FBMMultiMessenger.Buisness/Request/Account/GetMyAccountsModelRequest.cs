using FBMMultiMessenger.Contracts;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class GetMyAccountsModelRequest : IRequest<BaseResponse<PageableResponse<GetMyAccountsModelResponse>>>
    {
        public int PageNo { get; set; }
        public int PageSize { get; set; }
    }

    public class GetMyAccountsModelResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public bool IsActive { get; set; }
        public int TotalCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
