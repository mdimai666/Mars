using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Media.Abstractions.Dto.Files;

namespace Mars.Cms.Abstractions.Dto.PostJsons;

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
