using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Diagnostics;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/LoggerNode/LoggerNode{.lang}.md")]
[Display(GroupName = "diagnostic")]
public class LoggerNode : Node
{
    public override string TypeId => "core.LoggerNode";

    public ELoggerNodeLogLevel Level { get; set; } = ELoggerNodeLogLevel.Warning;

    public LoggerNode()
    {
        Inputs = [new()];
        Color = "#e9d585";
        hasTailButton = false;
        Icon = "_content/Mars.Nodes.Workspace/nodes/journal-text.svg";
    }

}
public enum ELoggerNodeLogLevel : int
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6

}
