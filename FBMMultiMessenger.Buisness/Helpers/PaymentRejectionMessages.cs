namespace FBMMultiMessenger.Buisness.Helpers
{
    public static class PaymentRejectionMessages
    {
        public static readonly string AMOUNT_LESS_THAN_REQUIRED =
            "Your payment was rejected because the amount paid is less than the required subscription price. Please ensure you pay the exact amount specified for your subscription plan.";

        public static readonly string AMOUNT_GREATER_THAN_REQUIRED =
            "Your payment was rejected because the amount paid exceeds the required subscription price. Please pay only the exact amount specified for your subscription plan.";

        public static readonly string CURRENCY_MISMATCH =
            "Your payment was rejected because the currency used does not match the required currency for this subscription. Please make the payment in the correct currency as specified.";

        public static readonly string PROOF_NOT_VISIBLE =
            "Your payment was rejected because the payment proof image you submitted is not clear or visible. Please upload a clear, high-quality image of your payment receipt or proof.";

        public static readonly string PROOF_TAMPERED =
            "Your payment was rejected because the payment proof appears to have been edited or tampered with. Please submit an original, unaltered screenshot or receipt of your payment.";

        public static readonly string INCOMPLETE_INFORMATION =
            "Your payment was rejected because the payment details provided are incomplete or missing required information. Please ensure all necessary payment information is clearly visible in your submission.";

        public static readonly string RECIPIENT_ACCOUNT_INCORRECT =
            "Your payment was rejected because the payment was not sent to the correct recipient account. Please verify the account details and send the payment to the specified account.";

        public static readonly string PAYMENT_DATE_MISMATCH =
            "Your payment was rejected because the payment date does not match your subscription request date. Please ensure the payment is made within the specified timeframe.";

        public static readonly string SUSPECTED_FRAUD =
            "Your payment was rejected due to suspected fraudulent activity. If you believe this is an error, please contact our support team with additional verification documents.";

        public static readonly string DUPLICATE_SUBMISSION =
            "Your payment was rejected because the same payment proof has been submitted multiple times. Each payment proof can only be used once. If you made a new payment, please submit the new receipt.";

        public static readonly string OTHER =
            "Your payment was rejected for reasons specified by our verification team. Please check the additional notes provided or contact support for more information.";
    }
}
