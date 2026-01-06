using Mars.Core.Models;

namespace Mars.Shared.Contracts.XActions;

public record XActionCommand
{
    public required string Id { get; init; }
    public required string Label { get; init; }

    public XActionType Type { get; init; }
    public string? LinkValue { get; init; }

    public string? KeybindingContext { get; init; }
    public int[]? Keybindings { get; init; }

    public string ContextMenuGroupId { get; init; } = "";
    public float ContextMenuOrder { get; init; }
    public string[]? FrontContextId { get; init; }

}

public class XActResult : IUserActionResult
{
    public enum XActionNextStep : int
    {
        Toast = 0,
        TriggerEvent,
        NextAction
    }

    public XActionNextStep NextStep { get; init; }
    public bool Ok { get; init; }
    public required string Message { get; init; }
    public MessageIntent MessageIntent { get; init; }
    public string? NextActionId { get; init; }

    public static XActResult ToastSuccess(string message)
        => new() { Ok = true, Message = message, MessageIntent = MessageIntent.Success, NextStep = XActionNextStep.Toast };

    public static XActResult ToastError(string message)
        => new() { Message = message, MessageIntent = MessageIntent.Error, NextStep = XActionNextStep.Toast };

    public static XActResult ToastWarning(string message)
        => new() { Message = message, MessageIntent = MessageIntent.Warning, NextStep = XActionNextStep.Toast };

    public static XActResult ToastInfo(string message)
        => new() { Message = message, MessageIntent = MessageIntent.Info, NextStep = XActionNextStep.Toast };
}

public record XActionCommandCall
{
    public required string Id { get; init; }
    public required string[] Args { get; init; }
}

public interface IActContext
{
    public string[] args { get; }
}

public interface IAct
{
    public Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken);
}

public enum XActionType : int
{
    Link = 0,
    HostAction = 1,
}
