using Mars.Server.Abstractions.Models;
using Mars.SiteEngine.Abstractions.Templators;
using Mars.Server.Contracts.Options;

namespace Mars.SiteEngine.Abstractions.WebSite.Models;

public class PageRenderContext
{
    public required RenderContextUser? User { get; init; }
    public List<string> BodyClass { get; init; } = [];
    public List<string> BodyAttrs { get; init; } = [];
    public required SiteSettings SiteSettings { get; init; }
    public required WebClientRequest Request { get; init; } = default!;
    public Dictionary<string, object?> TemplateContextVaribles { get; init; } = [];
    public required bool IsDevelopment { get; init; }

    public Dictionary<string, DataQueryRequest>? DataQueries { get; init; } = [];

    public required RenderParam RenderParam { get; init; }

    public List<PageRenderError> Errors { get; init; } = [];

    public XInterpreter CreateInterpreter(Dictionary<string, object>? localVaribles = null) => new(this, localVaribles);
}
