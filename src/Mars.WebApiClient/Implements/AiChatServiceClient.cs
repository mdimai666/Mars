using Flurl.Http;
using Flurl.Http.Content;
using Mars.AiChat.Contracts.Dto;
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

    public Task<AiChatSessionSummary> CreateSession(string? title = null, string? connectionName = null)
        => _client.Request($"{_basePath}{_controllerName}", "sessions")
                    .PostJsonAsync(new AiChatCreateSessionRequest { Title = title, ConnectionName = connectionName })
                    .ReceiveJson<AiChatSessionSummary>();

    public Task<AiChatSessionDto> GetSession(Guid chatId)
        => _client.Request($"{_basePath}{_controllerName}", "sessions", chatId)
                    .OnError(OnStatus404ThrowException)
                    .GetJsonAsync<AiChatSessionDto>();

    public Task<IReadOnlyList<AiChatConnectionDto>> GetConnections()
        => _client.Request($"{_basePath}{_controllerName}", "connections")
                    .GetJsonAsync<IReadOnlyList<AiChatConnectionDto>>();

    public Task<AiChatSessionDto> SetConnection(Guid chatId, string? connectionName)
        => _client.Request($"{_basePath}{_controllerName}", "sessions", chatId, "connection")
                    .OnError(OnStatus404ThrowException)
                    .PutJsonAsync(new AiChatSetConnectionRequest { ConnectionName = connectionName })
                    .ReceiveJson<AiChatSessionDto>();

    public Task DeleteSession(Guid chatId)
        => _client.Request($"{_basePath}{_controllerName}", "sessions", chatId)
                    .DeleteAsync();

    public Task<AiChatAttachmentDto> UploadAttachment(Stream fileStream, string fileName)
        => _client.Request($"{_basePath}{_controllerName}", "attachments")
                    .PostMultipartAsync(mp => mp.AddFile("file", fileStream, fileName))
                    .ReceiveJson<AiChatAttachmentDto>();

    public Task Send(Guid chatId, string message, string? pageContext = null, IReadOnlyList<Guid>? attachmentIds = null)
        => _client.Request($"{_basePath}{_controllerName}", "sessions", chatId, "send")
                    .OnError(OnStatus404ThrowException)
                    .PostJsonAsync(new AiChatSendRequest { Message = message, PageContext = pageContext, AttachmentIds = attachmentIds });

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
