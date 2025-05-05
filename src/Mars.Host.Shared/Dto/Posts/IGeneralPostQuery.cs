namespace Mars.Host.Shared.Dto.Posts;

public interface IGeneralPostQuery
{
    string Title { get; }
    string Type { get; }
    string Slug { get; }
    IReadOnlyCollection<string> Tags { get; }
    string Status { get; }
    Guid UserId { get; }
    string? Excerpt { get; }
    string? Content { get; }
    string LangCode { get; }
}
