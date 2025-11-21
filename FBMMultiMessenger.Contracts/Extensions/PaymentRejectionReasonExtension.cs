using FBMMultiMessenger.Contracts.CustomDataAnnotations;
using FBMMultiMessenger.Contracts.Enums;
using System.Reflection;

namespace FBMMultiMessenger.Contracts.Extensions
{
    public static class PaymentRejectionReasonExtension
    {
        public static (string Name, string Description) GetInfo(this PaymentRejectionReason paymentRejection)
        {
            var type = paymentRejection.GetType();
            var member = type.GetMember(paymentRejection.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttribute<RejectionReasonAttribute>();

            return attr != null
                ? (attr.Name, attr.Description)
                : (paymentRejection.ToString(), string.Empty);
        }
    }
}
