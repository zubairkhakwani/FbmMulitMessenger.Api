namespace FBMMultiMessenger.Contracts.Contracts.ApiKey
{
    public class GetMyApiKeyHttpRequest
    {

    }

    public class GetMyApiKeyHttpResponse
    {
        public int Id { get; set; }

        public string Key { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
