namespace FBMMultiMessenger.Contracts.Contracts.LocalServer
{
    public class GetLocalServerAccountsHttpResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public string? DefaultMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
