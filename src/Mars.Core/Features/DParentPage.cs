namespace Mars.Core.Features;

[System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
sealed public class DParentPageAttribute : Attribute
{
    // See the attribute guidelines at 
    //  http://go.microsoft.com/fwlink/?LinkId=85236
    readonly Type parentPage;

    // This is a positional argument
    public DParentPageAttribute(Type parentPage)
    {
        this.parentPage = parentPage;

    }

    public Type ParentPage
    {
        get { return parentPage; }
    }

}
