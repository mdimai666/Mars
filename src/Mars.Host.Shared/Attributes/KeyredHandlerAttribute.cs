namespace Mars.Host.Shared.Attributes;

[System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
public sealed class KeyredHandlerAttribute : Attribute
{
    public string Key { get; }
    public string[] Tags { get; init; } = [];
    public string Description { get; init; } = string.Empty;

    public KeyredHandlerAttribute(string key)
    {
        Key = key;
    }
}
