namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class Settings
    {
        public int Id { get; set; }
        public string Extension_Version { get; set; } = string.Empty;
        /// <summary>Gemini marketplace system prompt. Empty falls back to the built-in default.</summary>
        public string AiSystemPrompt { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
