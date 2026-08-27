using Mars.AiChat.Contracts.Options;

namespace Mars.AiChat.Contracts.Dto;

/// <summary>
/// Публичное представление подключения к ИИ-сервису (без секртов: endpoint/ключ не отдаём).
/// </summary>
public class AiChatConnectionDto
{
    public string Name { get; set; } = "";
    public AiProviderType ProviderType { get; set; }
    public string ModelId { get; set; } = "";
    public bool IsDefault { get; set; }
}
