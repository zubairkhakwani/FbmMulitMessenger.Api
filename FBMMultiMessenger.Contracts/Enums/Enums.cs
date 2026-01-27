using FBMMultiMessenger.Contracts.CustomDataAnnotations;
using System.ComponentModel.DataAnnotations;

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
        SuperAdmin = 3,
        SuperServer = 4
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

    public enum ContactSubject
    {
        [Display(Name = "General Inquiry")]
        General_Inquiry = 1,

        [Display(Name = "Technical Support")]
        Technical_Support = 2,

        [Display(Name = "Billing & Payment")]
        Billing_Payment = 3,

        [Display(Name = "Feature Request")]
        Feature_Request = 4,

        [Display(Name = "Bug Report")]
        Bug_Report = 5,

        [Display(Name = "Partnership Opportunity")]
        Partnership = 6,

        [Display(Name = "Sales Inquiry")]
        Sales_Inquiry = 7,

        [Display(Name = "Account Issue")]
        Account_Issue = 8,

        [Display(Name = "Feedback & Suggestions")]
        Feedback = 9,

        [Display(Name = "Privacy & Security")]
        Privacy_Security = 10,

        [Display(Name = "Refund Request")]
        Refund_Request = 11,

        [Display(Name = "Product Information")]
        Product_Information = 12,

        [Display(Name = "Other")]
        Other = 13
    }


    public enum AccountConnectionStatus
    {
        [DisplayInfoAttribute("Online", "The account is running and actively in use.")]
        Online = 1,

        [DisplayInfoAttribute("Offline", "The account is idle and not currently assigned to any server.")]
        Offline = 2,

        [DisplayInfoAttribute("Starting", "The account is launching and preparing to run on a server.")]
        Starting = 3,
    }

    public enum AccountAuthStatus
    {
        [DisplayInfoAttribute("Idle", "The login state has not been checked yet.")]
        Idle = 1,

        [DisplayInfoAttribute("Logged In", "The account is authenticated and the user is logged in.")]
        LoggedIn = 2,

        [DisplayInfoAttribute("Logged Out", "The account is not authenticated or the user has logged out.")]
        LoggedOut = 3,
    }

    public enum AccountSkipReason
    {
        [DisplayInfo("Duplicate Cookie", "Account has a cookie that already exists in the system.")]
        DuplicateCookie,

        [DisplayInfo("Invalid Proxy", "The assigned proxy ID does not exist or is not reachable.")]
        InvalidProxyId,

        [DisplayInfo("Unauthorized Proxy", "The proxy is blocked, banned, or not authorized for this account.")]
        UnauthorizedProxy,

        [DisplayInfo("Missing Proxy", "No proxy was assigned but one is required for this operation.")]
        MissingRequiredProxy,

        [DisplayInfo("Invalid Cookie", "The provided cookie is expired, malformed, or rejected.")]
        InvalidCookie,

        [DisplayInfo("Account Already Exists", "An account with the same unique identifiers already exists.")]
        AccountAlreadyExists
    }
    public enum BillingCylce
    {
        Monthly,
        SemiAnnual,
        Annual
    }


}
