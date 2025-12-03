using AppFront.Shared.Interfaces;

namespace Mars.WebSiteProcessor.Blazor.Services;

internal class MarsHostBlazorPrerenderHttpAccessor : IMarsHostBlazorPrerenderHttpAccessor
{
    public int? StatusCode { get; set; }
}
