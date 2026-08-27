using Mars.Contracts.Common;

namespace Mars.Cms.Abstractions.Dto.PostTypes;

public record ListPostTypeQuery : BasicListQuery
{
    /// <summary>Показывать встроенные типы-компоненты (по умолчанию скрыты)</summary>
    public bool IncludeComponent { get; init; }
}
