namespace Mars.Shared.Contracts.MetaFields;

public abstract record MetaFieldDetailBase
{
    public required Guid Id { get; init; }
    public required Guid ParentId { get; init; }
    public required string Title { get; init; }
    public required string Key { get; init; }
    public required MetaFieldType Type { get; init; }

    public required decimal? MaxValue { get; init; }
    public required decimal? MinValue { get; init; }
    public required string Description { get; init; }
    public required bool IsNullable { get; init; }
    //public required MetaValueDto? Default { get; init; }
    public required int Order { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required bool Hidden { get; init; }
    public required bool Disabled { get; init; }

    public required string? ModelName { get; init; }
}
