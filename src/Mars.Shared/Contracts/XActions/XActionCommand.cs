using Mars.Core.Models;

namespace Mars.Shared.Contracts.XActions;

public class XActionCommand
{
    public string Id { get; init; }
    public string Label { get; init; }

    public XActionType Type { get; init; }
    public string? LinkValue { get; init; }

    public string? KeybindingContext { get; init; }
    public int[]? Keybindings { get; init; }

    public string ContextMenuGroupId { get; init; }
    public float ContextMenuOrder { get; init; }
    public string[]? FrontContextId { get; init; }

    //[JsonIgnore]
    //public Func<object> Run { get; init; }

    public override bool Equals(object? obj)
    {
        if (obj is XActionCommand act)
            return Id == act.Id;

        return base.Equals(obj);
    }

    public override int GetHashCode() => (Id, 0).GetHashCode();
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
    public string Message { get; init; }
    public MessageIntent MessageIntent { get; init; }
    public string? NextActionId { get; init; }

    public static XActResult ToastSuccess(string message)
        => new() { Ok = true, Message = message, MessageIntent = MessageIntent.Success, NextStep = XActionNextStep.Toast };
    public static XActResult ToastError(string message)
        => new() { Message = message, MessageIntent = MessageIntent.Error, NextStep = XActionNextStep.Toast };
}

public class XActionCommandCall
{
    public string Id { get; init; }
    public string[] Args { get; init; }
}

public interface IActContext
{
    public string[] args { get; }
}

public interface IAct
{
    public Task<XActResult> Execute(IActContext context);
}

public enum XActionType : int
{
    Link = 0,
    HostAction = 1,
}
