namespace Mars.SiteEngine.Abstractions.Dto.Renders;

public class PostRenderDto
{
    public string Html { get; set; } = "";
    public string Title { get; set; } = "";
    public string EditUrl { get; set; } = "";
    public string PostSlug { get; set; } = "";
    public string PostType { get; set; } = "";
    public string TemplateId { get; set; } = "";
    public string DataId { get; set; } = "";
    public DateTime? Date { get; set; }

    public PostRenderDto()
    {

    }
}
