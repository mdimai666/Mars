using Mars.Contracts.ActionHistorys;

namespace Mars.Server.Abstractions.Services;

public interface IActionHistoryService
{
    public Task Add(Exception exception, string title);
    public Task Add<T>(T data, string title, ActionHistoryLevel level, string actionType, string? message = null)
        where T : class;

}
