using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/LoggerNode/LoggerNode{.lang}.md")]
public class LoggerNode : Node
{
    public ELoggerNodeLogLevel Level { get; set; } = ELoggerNodeLogLevel.Warning;

    public LoggerNode()
    {
        HaveInput = true;
        Color = "#e9d585";
        hasTailButton = false;
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
