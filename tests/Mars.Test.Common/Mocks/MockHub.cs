using Mars.Host.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Mars.Test.Common.Mocks;

public class ChatHubMock : IHubContext<ChatHub>
{
    public IHubClients Clients { get; } = new MockHubClients();
    public IGroupManager Groups { get; } = new MockGroupManager();
}

public class MockHubClients : IHubClients
{
    public IClientProxy All { get; } = new MockClientProxy();

    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
    {
        return All;
    }

    public IClientProxy Client(string connectionId)
    {
        return All;
    }

    public IClientProxy Clients(IReadOnlyList<string> connectionIds)
    {
        return All;
    }

    public IClientProxy Group(string groupName)
    {
        return All;
    }

    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
    {
        return All;
    }

    public IClientProxy Groups(IReadOnlyList<string> groupNames)
    {
        return All;
    }

    public IClientProxy User(string userId)
    {
        return All;
    }

    public IClientProxy Users(IReadOnlyList<string> userIds)
    {
        return All;
    }
}

public class MockGroupManager : IGroupManager
{
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class MockClientProxy : IClientProxy
{
    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
