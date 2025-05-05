namespace Mars.Nodes.Core.Attributes;

[System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class NodeEditFormForNodeAttribute : Attribute
{
    // See the attribute guidelines at 
    //  http://go.microsoft.com/fwlink/?LinkId=85236
    readonly Type forNodeType;

    // This is a positional argument
    public NodeEditFormForNodeAttribute(Type forNodeType)
    {
        this.forNodeType = forNodeType;

    }

    public Type ForNodeType
    {
        get { return forNodeType; }
    }

}

