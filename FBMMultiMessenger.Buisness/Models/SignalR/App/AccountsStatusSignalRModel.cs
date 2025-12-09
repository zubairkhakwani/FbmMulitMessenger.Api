using Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal;

namespace FBMMultiMessenger.Buisness.Models.SignalR.App
{
    public class AccountsStatusSignalRModel
    {
        public Dictionary<int, string> AccountStatus { get; set; } = new Dictionary<int, string>();
    }
}
