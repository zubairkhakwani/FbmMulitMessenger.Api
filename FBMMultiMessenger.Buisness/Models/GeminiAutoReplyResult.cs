namespace FBMMultiMessenger.Buisness.Models
{
    public class GeminiAutoReplyResult
    {
        /// <summary>Buyer-facing text written by the model (never hardcoded).</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Seconds to wait before sending (0–8).</summary>
        public int DelaySeconds { get; set; }

        /// <summary>When true, attach the product's saved images with the message.</summary>
        public bool SendProductImages { get; set; }

        /// <summary>
        /// Product the model matched from listing title / buyer intent.
        /// Required when multiple products were in the prompt so the app can attach the right photos.
        /// </summary>
        public int? MatchedProductId { get; set; }
    }
}
