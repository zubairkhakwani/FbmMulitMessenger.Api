using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class ImportAccountsModelRequest : IRequest<BaseResponse<UpsertAccountModelResponse>>
    {
        public List<ImportAccounts> Accounts { get; set; } = new List<ImportAccounts>();
    }

    public class ImportAccounts
    {
        public string Name { get; set; } = null!;

        public string Cookie { get; set; } = null!;

        public string FbAccountId { get; set; } = string.Empty; //This will be generated in the handler by parsing the cookie so no need to pass fbAccountId
    }
}
