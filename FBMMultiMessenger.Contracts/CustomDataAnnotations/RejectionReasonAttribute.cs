namespace FBMMultiMessenger.Contracts.CustomDataAnnotations
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class RejectionReasonAttribute : Attribute
    {
        public string Name { get; } = string.Empty;
        public string Description { get; } = string.Empty;

        public RejectionReasonAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
