using System.ComponentModel.DataAnnotations;

namespace Mars.AiChat.Contracts.Options;

/// <summary>
/// Настройка подключения к одному ИИ-сервису (OpenAI, Qwen, DeepSeek, Ollama, любой OpenAI-совместимый).
/// </summary>
public sealed class AiProviderConnection
{
    [Required]
    [Display(Name = "Название")]
    public string Name { get; set; } = "";

    [Display(Name = "Провайдер")]
    public AiProviderType ProviderType { get; set; } = AiProviderType.OpenAI;

    [Display(Name = "Endpoint")]
    public string Endpoint { get; set; } = "";

    [Display(Name = "API ключ")]
    public string ApiKey { get; set; } = "";

    [Required]
    [Display(Name = "Модель")]
    public string ModelId { get; set; } = "";

    /// <summary>
    /// Endpoint с применением значения по умолчанию для провайдера.
    /// </summary>
    public string ResolveEndpoint()
        => string.IsNullOrWhiteSpace(Endpoint) ? ProviderType.GetDefaultEndpoint() : Endpoint.TrimEnd('/');
}
