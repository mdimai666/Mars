namespace Mars.Host.Shared.Attributes;

[System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
public sealed class RegisterAIToolAttribute : Attribute
{
    public string? Key { get; init; }
    public string[] Tags { get; init; } = [];
    public string Description { get; init; } = string.Empty;
}
