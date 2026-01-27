namespace Mars.Shared.Contracts.Renders;

public class PostRenderResponse
{
    public required string Html { get; set; }
    public required string Title { get; init; }
    public required string EditUrl { get; init; }
    public required string PostSlug { get; init; }
    public required string PostType { get; init; }
    public required string TemplateId { get; init; }
    public required string DataId { get; init; }
    public required DateTime? Date { get; init; }

}
