using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Buisness.Models.SignalR.LocalServer
{
    //This class is responsible for sending account details to user's local server.
    public class LocalServerAccountDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Cookie { get; set; } = string.Empty;
        public string? DefaultMessage { get; set; }
        public AccountRestartReason RestartReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public LocalServerProxyDTO? Proxy { get; set; }

    }

    public class LocalServerProxyDTO
    {
        public int Id { get; set; }
        public string Ip_Port { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
