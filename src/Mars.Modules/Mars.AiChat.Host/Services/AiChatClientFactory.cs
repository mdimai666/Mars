using System.ClientModel;
using Mars.AiChat.Host.Shared.Interfaces;
using Mars.AiChat.Shared.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace Mars.AiChat.Host.Services;

/// <summary>
/// Создаёт IChatClient по настройке подключения.
/// Ollama — нативный клиент; остальные провайдеры — через OpenAI-совместимый API.
/// </summary>
public class AiChatClientFactory : IAiChatClientFactory
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, IChatClient> _clients = new();
    private readonly ILogger<AiChatClientFactory> _logger;

    public AiChatClientFactory(ILogger<AiChatClientFactory> logger)
    {
        _logger = logger;
    }

    public IChatClient CreateClient(AiProviderConnection connection)
    {
        ArgumentException.ThrowIfNullOrEmpty(connection.ModelId, nameof(connection.ModelId));

        var key = $"{connection.ProviderType}|{connection.ResolveEndpoint()}|{connection.ApiKey}|{connection.ModelId}";
        return _clients.GetOrAdd(key, _ =>
        {
            _logger.LogDebug("AiChat: creating {ProviderType} client, endpoint {Endpoint}, model '{ModelId}'",
                connection.ProviderType, connection.ResolveEndpoint(), connection.ModelId);
            return Build(connection);
        });
    }

    static IChatClient Build(AiProviderConnection connection)
    {
        if (connection.ProviderType == AiProviderType.Ollama)
        {
            return new OllamaChatClient(new Uri(connection.ResolveEndpoint()), connection.ModelId);
        }

        // OpenAI, Qwen (DashScope), DeepSeek и любой OpenAI-совместимый endpoint
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(connection.ResolveEndpoint()),
        };

        // Локальные серверы (LM Studio, vLLM и т.п.) могут не требовать ключ —
        // OpenAI SDK требует непустой credential, поэтому ставим заглушку.
        var credential = new ApiKeyCredential(string.IsNullOrWhiteSpace(connection.ApiKey) ? "no-key" : connection.ApiKey);
        var openAiClient = new OpenAIClient(credential, options);

        return openAiClient.GetChatClient(connection.ModelId).AsIChatClient();
    }
}
