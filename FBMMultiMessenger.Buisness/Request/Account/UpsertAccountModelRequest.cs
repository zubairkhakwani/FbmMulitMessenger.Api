using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class UpsertAccountModelRequest : IRequest<BaseResponse<UpsertAccountModelResponse>>
    {
        public int? AccountId { get; set; }
        public int UserId { get; set; } //Current User Id
        public string Name { get; set; } = null!;

        public string Cookie { get; set; } = null!;
    }

    public class UpsertAccountModelResponse
    {
        public bool IsLimitExceeded { get; set; }
        public bool IsSubscriptionExpired { get; set; }

        public bool IsEmailVerified { get; set; }
        public string EmailSendTo { get; set; } = string.Empty;
    }
}
