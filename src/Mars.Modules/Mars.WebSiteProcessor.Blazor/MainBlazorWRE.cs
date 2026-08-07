using AppFront.Shared.Interfaces;
using Mars.WebSiteProcessor.Blazor.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebSiteProcessor.Blazor;

public static class MainBlazorWRE
{
    public static WebApplicationBuilder AddWREBlazor(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<HtmlRenderer>();
        builder.Services.AddScoped<BlazorRenderer>();
        builder.Services.AddScoped<IMarsHostBlazorPrerenderHttpAccessor, MarsHostBlazorPrerenderHttpAccessor>();

        return builder;
    }
}
