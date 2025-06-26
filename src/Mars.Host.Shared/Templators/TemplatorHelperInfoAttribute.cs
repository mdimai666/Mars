namespace Mars.Host.Shared.Templators;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class TemplatorHelperInfoAttribute : Attribute
{
    public string Shortcut { get; }
    public string Example { get; }
    public string Description { get; }

    public TemplatorHelperInfoAttribute(string shortcut, string example, string description)
    {
        Shortcut = shortcut;
        Example = example;
        Description = description;
    }
}
