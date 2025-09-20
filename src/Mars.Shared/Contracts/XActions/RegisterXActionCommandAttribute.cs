namespace Mars.Shared.Contracts.XActions;

[System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RegisterXActionCommandAttribute : Attribute
{
    public string ActionId { get; }
    public string Label { get; }

    /// <summary>
    /// see <see cref="XActionCommand"/>
    /// </summary>
    /// <param name="actionId"></param>
    /// <param name="label"></param>
    public RegisterXActionCommandAttribute(string actionId, string label)
    {
        ActionId = actionId;
        Label = label;
    }

}
