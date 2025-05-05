namespace Mars.Host.Shared.Dto.NavMenus;

public record NavMenuSummary
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required bool Disabled { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
}
