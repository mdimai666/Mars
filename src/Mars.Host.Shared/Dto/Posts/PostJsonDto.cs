using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.Posts;

public record PostJsonDto : PostSummary
{
    public required string? Content { get; init; }

    /// <summary>
    /// json Dto also may be
    /// <list type="bullet">
    /// <item><see cref="MetaFieldVariantValueDto"/></item>
    /// <item><see cref="MetaFieldVariantValueDto"/>[]</item>
    /// <item><see cref="FileDetail"/></item>
    /// </list>
    /// <inheritdoc cref="MetaValueDto.Value"/>
    /// </summary>
    public required Dictionary<string, object?> Meta { get; init; }
    
}
