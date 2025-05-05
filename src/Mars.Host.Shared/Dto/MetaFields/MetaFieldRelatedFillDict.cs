using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Dto.MetaFields;

public record MetaFieldRelatedFillDictValue
{
    public required Guid ModelId { get; init; }
    public required MetaFieldType Type { get; init; }
    public object? ModelDto { get; set; }
    public required string? ModelName { get; init; }
}

public class MetaFieldRelatedFillDict : Dictionary<(MetaFieldType type, string? modelName, Guid ModelId), MetaFieldRelatedFillDictValue>
{
    public MetaFieldRelatedFillDict()
    {
    }

    public MetaFieldRelatedFillDict(int capacity) : base(capacity)
    {
    }

    public MetaFieldRelatedFillDict(IEnumerable<KeyValuePair<(MetaFieldType type, string? modelName, Guid ModelId), MetaFieldRelatedFillDictValue>> values) : base(values)
    {

    }
}
