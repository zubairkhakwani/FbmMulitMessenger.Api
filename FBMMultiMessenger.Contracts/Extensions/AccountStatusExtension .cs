using FBMMultiMessenger.Contracts.CustomDataAnnotations;
using FBMMultiMessenger.Contracts.Enums;
using System.Reflection;

namespace FBMMultiMessenger.Contracts.Extensions
{
    public static class AccountStatusExtension
    {
        public static (string Name, string Description) GetInfo(this AccountStatus accountStatus)
        {
            var type = accountStatus.GetType();
            var member = type.GetMember(accountStatus.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttribute<AccountStatusAttribute>();

            return attr != null
                ? (attr.Name, attr.Description)
                : (accountStatus.ToString(), string.Empty);
        }
    }
}
