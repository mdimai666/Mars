using Mars.Shared.Validators;

namespace Mars.Host.Shared.Dto.PostTypes;

public record PostTypeSummary
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }
    //public required ModelViewSettings ViewSettings { get; init; }
}

public record ModelViewSettings
{
    [ValidateSourceUri]
    public required string? ListViewTemplateSourceUri { get; init; }
}
