namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class UpdateAccountCustomNameHttpRequest
    {
        public string CustomName { get; set; } = string.Empty;
    }

    public class UpdateAccountCustomNameHttpResponse
    {
        public int AccountId { get; set; }
        public string CustomName { get; set; } = string.Empty;
    }
}
