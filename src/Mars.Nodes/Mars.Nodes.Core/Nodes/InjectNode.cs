using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/InjectNode/InjectNode{.lang}.md")]
public class InjectNode : Node
{
    public string Payload { get; set; } = "";

    [Display(Name = "Run at startup")]
    public bool RunAtStartup { get; set; }

    [Display(Name = "Delay millis")]
    public int StartupDelayMillis { get; set; }


    public bool IsSchedule { get; set; }
    public string ScheduleCronMask { get; set; } = "0 0/10 * * * ?";

    public InjectNode()
    {
        isInjectable = true;
        Color = "#A9BBCF";
        Outputs = new List<NodeOutput> { new NodeOutput() };
    }

}


class InputSource<T>
{
#pragma warning disable CS0414 // The field 'InputSource<T>.Type' is assigned but its value is never used
    string Type = default!;
    T Value = default!;
#pragma warning restore CS0414 // The field 'InputSource<T>.Type' is assigned but its value is never used
    object get()
    {
        throw new NotImplementedException();
    }

    void sdsd()
    {


        object z = get();
    }

}

class InputSource
{
    //string Name;

}

enum InputType
{
    String,
    Number,
    Boolean,
    DateTime,
    Flow,
    Global,


}

public class ConfigNode
{

}

public class DrawNode
{

}
