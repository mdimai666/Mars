using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.PostTypes;

public record PostContentSettingsDto
{
    /// <summary>
    /// <see cref="PostTypeConstants.DefaultPostContentTypes.PlainText"/>
    /// </summary>
    public required string PostContentType { get; init; } 
    public required string? CodeLang { get; init; }
}
