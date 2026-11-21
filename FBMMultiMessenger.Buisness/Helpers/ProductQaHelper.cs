using System.Text.Json;

namespace FBMMultiMessenger.Buisness.Helpers
{
    internal static class ProductQaHelper
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static string SerializeQuestions(IEnumerable<string> questions)
        {
            var cleaned = questions
                .Select(q => (q ?? string.Empty).Trim())
                .Where(q => q.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return JsonSerializer.Serialize(cleaned, JsonOptions);
        }

        public static List<string> DeserializeQuestions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
