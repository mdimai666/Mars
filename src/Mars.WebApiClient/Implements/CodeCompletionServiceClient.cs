using Flurl.Http;
using Mars.CodeCompletion.Contracts.Dto;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class CodeCompletionServiceClient : BasicServiceClient, ICodeCompletionServiceClient
{
    public CodeCompletionServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "CodeCompletion";
    }

    public Task<CodeCompletionInfo?> GetInfo()
        => _client.Request($"{_basePath}{_controllerName}", "info")
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<CodeCompletionInfo?>();

    public Task<CompletionResponseDto> GetCompletions(string contextId, CodePositionRequest request)
        => _client.Request($"{_basePath}{_controllerName}", contextId, "completion")
                    .PostJsonAsync(request)
                    .ReceiveJson<CompletionResponseDto>();

    public Task<HoverResponseDto?> GetHover(string contextId, CodePositionRequest request)
        => _client.Request($"{_basePath}{_controllerName}", contextId, "hover")
                    .OnError(OnStatus404ReturnNull)
                    .PostJsonAsync(request)
                    .ReceiveJson<HoverResponseDto?>();

    public Task<SignatureHelpResponseDto?> GetSignatureHelp(string contextId, CodePositionRequest request)
        => _client.Request($"{_basePath}{_controllerName}", contextId, "signature")
                    .OnError(OnStatus404ReturnNull)
                    .PostJsonAsync(request)
                    .ReceiveJson<SignatureHelpResponseDto?>();

    public Task<IReadOnlyList<DiagnosticDto>> GetDiagnostics(string contextId, CodePositionRequest request)
        => _client.Request($"{_basePath}{_controllerName}", contextId, "diagnostics")
                    .PostJsonAsync(request)
                    .ReceiveJson<IReadOnlyList<DiagnosticDto>>();

    public Task RemoveDocument(string contextId, string documentId)
        => _client.Request($"{_basePath}{_controllerName}", contextId, "document", documentId)
                    .OnError(OnStatus404ReturnNull)
                    .DeleteAsync();
}
