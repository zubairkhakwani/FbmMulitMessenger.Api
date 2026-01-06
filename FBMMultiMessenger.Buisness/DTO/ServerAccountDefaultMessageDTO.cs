namespace FBMMultiMessenger.Buisness.DTO
{
    public class ServerAccountDefaultMessageDTO
    {
        // Key: Local server unique ID, Value: List of account messages for that server
        public Dictionary<string, List<DefaultMessageDTO>> AccountDefaultMessages { get; set; } = new Dictionary<string, List<DefaultMessageDTO>>();

        public class DefaultMessageDTO
        {
            public string FbAccountId { get; set; } = string.Empty;
            public string? DefaultMessage { get; set; }
        }
    }
}