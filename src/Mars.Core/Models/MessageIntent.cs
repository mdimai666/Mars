using System.ComponentModel;

namespace Mars.Core.Models;

public enum MessageIntent
{
    [Description("info")]
    Info,
    [Description("warning")]
    Warning,
    [Description("error")]
    Error,
    [Description("success")]
    Success,
    [Description("custom")]
    Custom
}
