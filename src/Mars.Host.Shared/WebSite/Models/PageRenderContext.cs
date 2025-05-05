using Mars.Host.Shared.Models;
using Mars.Shared.Options;

namespace Mars.Host.Shared.WebSite.Models;

public class PageRenderContext
{
    public required RenderContextUser? User { get; init; }
    public List<string> BodyClass { get; init; } = new();
    public List<string> BodyAttrs { get; init; } = new();
    public required SysOptions SysOptions { get; init; }
    public required WebClientRequest Request { get; init; } = default!;
    public Dictionary<string, object?> TemplateContextVaribles { get; init; } = new();
    public required bool IsDevelopment { get; init; }

    public Dictionary<string, DataQueryRequest>? DataQueries { get; init; } = new();

    public required RenderParam RenderParam { get; init; }
}
