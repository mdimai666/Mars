using Flurl.Http;
using Mars.AiChat.Shared.Dto;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class AiChatServiceClient : BasicServiceClient, IAiChatServiceClient
{
    public AiChatServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "AiChat";
    }

    public Task<IReadOnlyList<AiChatSessionSummary>> GetSessions()
        => _client.Request($"{_basePath}{_controllerName}", "sessions")
                    .GetJsonAsync<IReadOnlyList<AiChatSessionSummary>>();

    public Task<AiChatSessionSummary> CreateSession(string? title = null)
        => _client.Request($"{_basePath}{_controllerName}", "sessions")
                    .PostJsonAsync(new AiChatCreateSessionRequest { Title = title })
                    .ReceiveJson<AiChatSessionSummary>();

    public Task<AiChatSessionDto> GetSession(Guid chatId)
        => _client.Request($"{_basePath}{_controllerName}", "sessions", chatId)
                    .OnError(OnStatus404ThrowException)
                    .GetJsonAsync<AiChatSessionDto>();

    public Task DeleteSession(Guid chatId)
        => _client.Request($"{_basePath}{_controllerName}", "sessions", chatId)
                    .DeleteAsync();

    public Task Send(Guid chatId, string message)
        => _client.Request($"{_basePath}{_controllerName}", "sessions", chatId, "send")
                    .OnError(OnStatus404ThrowException)
                    .PostJsonAsync(new AiChatSendRequest { Message = message });

    public async Task<bool> Stop(Guid chatId)
    {
        var result = await _client.Request($"{_basePath}{_controllerName}", "sessions", chatId, "stop")
                            .PostAsync()
                            .ReceiveJson<StopResult>();
        return result.Stopped;
    }

    private class StopResult
    {
        public bool Stopped { get; set; }
    }
}
