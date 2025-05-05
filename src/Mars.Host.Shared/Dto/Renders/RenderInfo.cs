namespace Mars.Host.Shared.Dto.Renders;

public class RenderInfo
{
    public required string Template { get; init; }
    public required object Context { get; init; }
    public required string Title { get; init; }
    public required DateTime? Date { get; init; }

    public required string TemplateId { get; init; }
    public required string DataId { get; init; }

    public required string PostSlug { get; init; }
    public required string PostType { get; init; }

}
