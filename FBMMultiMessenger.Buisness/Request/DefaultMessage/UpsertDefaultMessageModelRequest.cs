using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using OneSignal.RestAPIv3.Client.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.DefaultMessage
{
    public class UpsertDefaultMessageModelRequest : IRequest<BaseResponse<UpsertDefaultMessageModelResponse>>
    {
        public int? Id { get; set; }
        public string Message { get; set; } = null!;

        public int CurrentUserId { get; set; }
        public List<int> SelectedAccounts { get; set; } = new List<int>();
    }

    public class UpsertDefaultMessageModelResponse { }

}
