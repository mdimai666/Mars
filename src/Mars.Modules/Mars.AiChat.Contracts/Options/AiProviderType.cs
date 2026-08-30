namespace Mars.AiChat.Contracts.Options;

/// <summary>
/// Тип ИИ-провайдера для подключения.
/// </summary>
public enum AiProviderType
{
    OpenAI = 0,
    Qwen = 1,
    DeepSeek = 2,
    Ollama = 3,
    Custom = 4,
}

public static class AiProviderTypeExtensions
{
    /// <summary>
    /// Endpoint по умолчанию для провайдера (OpenAI-совместимый API, кроме Ollama).
    /// </summary>
    public static string GetDefaultEndpoint(this AiProviderType providerType) => providerType switch
    {
        AiProviderType.OpenAI => "https://api.openai.com/v1",
        AiProviderType.Qwen => "https://dashscope.aliyuncs.com/compatible-mode/v1",
        AiProviderType.DeepSeek => "https://api.deepseek.com/v1",
        AiProviderType.Ollama => "http://localhost:11434",
        AiProviderType.Custom => "",
        _ => "",
    };
}
