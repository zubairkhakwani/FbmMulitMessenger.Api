namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class GetMyAccountsHttpRequest
    {
        public int PageNo { get; set; }
        public int PageSize { get; set; }
    }
    public class GetMyAccountsHttpResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
