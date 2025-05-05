using Mars.Shared.Contracts.ActionHistorys;

namespace Mars.Host.Shared.Services;

public interface IActionHistoryService
{
    public Task Add(Exception exception, string title);
    public Task Add<T>(T data, string title, ActionHistoryLevel level, string actionType, string? message = null)
        where T : class;

}
