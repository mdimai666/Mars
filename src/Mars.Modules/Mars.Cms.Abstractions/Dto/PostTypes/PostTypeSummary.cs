using Mars.Core.Interfaces;
using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Validators;

namespace Mars.Cms.Abstractions.Dto.PostTypes;

public record PostTypeSummary : IHasId
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required IReadOnlyCollection<string> EnabledFeatures { get; init; }
    public required bool Disabled { get; init; }
    public required PostTypeVisibility Visibility { get; init; }
    public string? ImageFieldKey { get; init; }

}

public record ModelViewSettings
{
    [ValidateSourceUri]
    public required string? ListViewTemplateSourceUri { get; init; }
}
