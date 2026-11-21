namespace FBMMultiMessenger.Buisness.Models
{
    /// <summary>One product (with listing titles + Q&As) offered to Gemini for matching.</summary>
    public class GeminiProductPromptItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public List<string> ListingTitles { get; set; } = new();
        public int ImageCount { get; set; }
        public List<GeminiProductQaItem> Qas { get; set; } = new();
    }
}
