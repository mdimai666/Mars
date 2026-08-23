using Mars.Shared.Common;

namespace Mars.Host.Shared.Dto.PostTypes;

public record ListPostTypeQuery : BasicListQuery
{
    /// <summary>Показывать встроенные типы-компоненты (по умолчанию скрыты)</summary>
    public bool IncludeComponent { get; init; }
}
