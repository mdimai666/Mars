using System.Diagnostics;

namespace Mars.Integration.Tests.Attributes;

public class ManualFactAttribute : FactAttribute
{
    public ManualFactAttribute()
    {
        if (!Debugger.IsAttached)
        {
            Skip = "Only running in interactive mode.";
        }
    }
}
