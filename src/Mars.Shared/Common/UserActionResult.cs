using System.ComponentModel;
using Mars.Core.Models;

namespace Mars.Shared.Common;

public class UserActionResult : IUserActionResult
{
    public static readonly string SuccessMessage = "Успешно выполнено";

    [DefaultValue(false)]
    public bool Ok { get; init; }
    public string Message { get; init; } = default!;
    public string[]? DetailMessages { get; init; }

    public static UserActionResult Success(string? message = null) => new() { Ok = true, Message = message ?? "Успешно выполнено" };
    public static UserActionResult SuccessDeleted() => new() { Ok = true, Message = "Успешно удалено" };

    public static UserActionResult Exception(Exception exception) => new() { Ok = false, Message = exception.Message };
    public static UserActionResult Exception(string message, string[]? detailMessages) => new() { Ok = false, Message = message, DetailMessages = detailMessages };
}

public class UserActionResult<TData> : IUserActionResult<TData>
{
    public bool Ok { get; set; }
    public string Message { get; set; } = default!;
    public required TData Data { get; set; }

    public static UserActionResult<TData> Success(TData data, string? message = null) => new() { Ok = true, Data = data, Message = message ?? "Успешно выполнено" };
    public static UserActionResult<TData> SuccessDeleted() => new() { Ok = true, Message = "Успешно удалено", Data = default! };

    public static UserActionResult<TData> Exception(Exception exception) => new() { Ok = false, Message = exception.Message, Data = default! };
}

public class RenderActionResult<TData> : UserActionResult<TData>
{
    public bool NotFound { get; set; }
}

public class UserLikeResult : UserActionResult
{
    public bool LikedState { get; set; }
    public int TotalLikes { get; set; }
}

public class ExecuteActionRequest
{
    public required string ActionId { get; set; }
    public required Dictionary<string, string> Arguments { get; set; }
}

//public static class UserActionResultExtensions
//{
//    public static UserActionResult<TData> AsUserActionResultSuccess<TData>(this TData data) where TData : IBasicEntity
//        => new UserActionResult<TData>() { Ok = true, Data = data, Message = UserActionResult.SuccessMessage };

//}
