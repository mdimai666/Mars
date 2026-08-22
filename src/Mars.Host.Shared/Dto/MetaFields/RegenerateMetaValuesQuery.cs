namespace Mars.Host.Shared.Dto.MetaFields;

/// <summary>Режим перегенерации значений генераторов у существующих постов</summary>
public enum MetaValuesRegenerationMode
{
    /// <summary>Перенумеровать все отобранные посты с 1 (по скоупам префиксов), существующие значения перезаписываются</summary>
    All,

    /// <summary>То же, но только для постов, созданных сегодня</summary>
    Today,

    /// <summary>Существующие значения не трогать — дозаполнить пустые, продолжая счётчик с последнего номера</summary>
    FromLast,
}

public record RegenerateMetaValuesQuery
{
    public required string PostTypeName { get; init; }
    public MetaValuesRegenerationMode Mode { get; init; } = MetaValuesRegenerationMode.All;

    /// <summary>Слуги статусов; пусто — все статусы</summary>
    public IReadOnlyCollection<string>? StatusSlugs { get; init; }
}

public record RegenerateMetaValuesResult(int PostsProcessed, int ValuesUpdated);
