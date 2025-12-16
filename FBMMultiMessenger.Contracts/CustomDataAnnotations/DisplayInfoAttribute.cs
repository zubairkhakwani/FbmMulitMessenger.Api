namespace FBMMultiMessenger.Contracts.CustomDataAnnotations
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class DisplayInfoAttribute : Attribute
    {
        public string Name { get; } = string.Empty;
        public string Description { get; } = string.Empty;

        public DisplayInfoAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
