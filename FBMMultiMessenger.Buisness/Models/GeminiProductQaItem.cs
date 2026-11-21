namespace FBMMultiMessenger.Buisness.Models
{
    public class GeminiProductQaItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string QuestionVariantsJson { get; set; } = "[]";
        public string Answer { get; set; } = string.Empty;
    }
}
