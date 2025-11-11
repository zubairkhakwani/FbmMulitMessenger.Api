using FBMMultiMessenger.Contracts.Shared;
using MediatR;
namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class ImportAccountsModelRequest : IRequest<BaseResponse<object>>
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
