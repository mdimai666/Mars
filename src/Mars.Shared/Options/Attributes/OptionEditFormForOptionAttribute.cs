namespace Mars.Shared.Options.Attributes;

[System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class OptionEditFormForOptionAttribute : Attribute
{
    // See the attribute guidelines at 
    //  http://go.microsoft.com/fwlink/?LinkId=85236
    readonly Type forOptionType;

    // This is a positional argument
    public OptionEditFormForOptionAttribute(Type forOptionType)
    {
        this.forOptionType = forOptionType;

    }

    public Type ForOptionType
    {
        get { return forOptionType; }
    }
}
