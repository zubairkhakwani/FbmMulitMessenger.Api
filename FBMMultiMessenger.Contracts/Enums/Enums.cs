using FBMMultiMessenger.Contracts.CustomDataAnnotations;

namespace FBMMultiMessenger.Contracts.Enums
{
    public enum PaymentStatus
    {
        Approved = 1,
        Pending = 2,
        Rejected = 3
    }
    public enum Roles
    {
        Customer = 1,
        Admin = 2,
        SuperAdmin = 3
    }

    public enum PaymentRejectionReason
    {
        [RejectionReason("Amount less than required", "Payment amount is less than the required subscription price")]
        AMOUNT_LESS_THAN_REQUIRED = 1,

        [RejectionReason("Amount greater than required", "Payment amount is greater than the subscription price")]
        AMOUNT_GREATER_THAN_REQUIRED = 2,

        [RejectionReason("Currency mismatch", "Payment currency does not match the required currency")]
        CURRENCY_MISMATCH = 3,

        [RejectionReason("Proof not visible", "Payment proof image is not clear or visible")]
        PROOF_NOT_VISIBLE = 4,

        [RejectionReason("Proof Tampered", "Payment proof appears to be edited or tampered with")]
        PROOF_TAMPERED = 6,

        [RejectionReason("Information incomplete", "Required payment details are incomplete or missing")]
        INCOMPLETE_INFORMATION = 7,

        [RejectionReason("Receipt account incorrect", "Payment was not sent to the correct recipient account")]
        RECIPIENT_ACCOUNT_INCORRECT = 15,

        [RejectionReason("Payment date mistmatch", "Payment date does not match subscription request date")]
        PAYMENT_DATE_MISMATCH = 17,

        [RejectionReason("Suspected fraud", "Payment shows signs of fraudulent activity")]
        SUSPECTED_FRAUD = 18,

        [RejectionReason("Duplicate Submission", "Same payment proof submitted multiple times")]
        DUPLICATE_SUBMISSION = 19,

        [RejectionReason("Other", "Other reason - see notes")]
        OTHER = 23
    }
}
