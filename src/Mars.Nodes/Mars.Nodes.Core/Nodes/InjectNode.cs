using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mars.Core.Attributes;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Fields;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/InjectNode/InjectNode{.lang}.md")]
[Display(GroupName = "common")]
public class InjectNode : Node
{
    [JsonConverter(typeof(InputValueJsonConverter<string>))]
    [TypeConverter(typeof(InputValueTypeConverter<string>))]
    public InputValue<string> Payload { get; set; } = "";
    //public InputValue<int> Payload2 { get; set; } = "333";
    //public InputValue<object> Payload3 { get; set; } = "333";

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
        Icon = "_content/Mars.Nodes.Workspace/nodes/box-arrow-in-right.svg";
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

public class DrawNode
{

}
