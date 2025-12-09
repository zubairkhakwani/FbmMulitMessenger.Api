using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.CustomDataAnnotations
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class AccountStatusAttribute :Attribute
    {
        public string Name { get; } = string.Empty;
        public string Description { get; } = string.Empty;

        public AccountStatusAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
