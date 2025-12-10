using System.Text.Json.Nodes;

namespace Mars.Host.Shared.Dto.PostJsons;

public record UpdatePostJsonQuery
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required Guid UserId { get; init; }
    public required string? Content { get; init; }
    public required string? Status { get; init; }

    public required string? Excerpt { get; init; }
    public required string LangCode { get; init; }

    public required IReadOnlyDictionary<string, JsonValue>? Meta { get; init; }
}
