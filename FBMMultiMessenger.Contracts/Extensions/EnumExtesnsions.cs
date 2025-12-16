using FBMMultiMessenger.Contracts.CustomDataAnnotations;
using System.Reflection;

namespace FBMMultiMessenger.Contracts.Extensions
{
    public static class EnumExtensions
    {
        public static (string Name, string Description) GetInfo<TEnum>(this TEnum enumValue)
            where TEnum : struct, Enum
        {
            var type = enumValue.GetType();
            var member = type.GetMember(enumValue.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttribute<DisplayInfoAttribute>();

            return attr != null
                ? (attr.Name, attr.Description)
                : (enumValue.ToString(), string.Empty);
        }
    }
}
