using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Dto.PostCategories;

public record PostCategoryEditDetail
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required string PostType { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }

    public required Guid? ParentId { get; init; }
    public required Guid[] PathIds { get; init; } = [];

    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<MetaValueDetailDto> MetaValues { get; init; }
}
