using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.LocalServer
{
    public class LocalServerHeartBeatHttpRequet
    {
        [Required]
        public string ServerId { get; set; } = null!;
        public List<int> ActiveAccountIds { get; set; } = new List<int>(); // Accounts running on this server
    }

    public class LocalServerHeartBeatHttpResponse
    {
    }
}
