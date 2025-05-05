namespace Mars.Core.Models;

public interface IUserActionResult
{
    public bool Ok { get; }
    public string Message { get; }
}

public interface IUserActionResult<TData> : IUserActionResult
{
    public TData Data { get; }
}
