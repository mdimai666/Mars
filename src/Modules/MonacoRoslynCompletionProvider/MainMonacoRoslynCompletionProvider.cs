using System.IO;
using System.Text.Json;
using Mars.Host.Shared.Constants.Website;
using Mars.Host.Shared.WebSite.Scripts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MonacoRoslynCompletionProvider.Dto;
using MonacoRoslynCompletionProvider.Providers;

namespace MonacoRoslynCompletionProvider;

public static class MonacoRoslynCompletionProviderHostExtensions
{
    public static WebApplicationBuilder AddRoslynCompletionProvider(this WebApplicationBuilder builder)
    {

        return builder;
    }

    public static IApplicationBuilder UseRoslynCompletionProvider(this WebApplication app)
    {

        var scripts = app.Services.GetRequiredKeyedService<IWebSitePluggablePluginScripts>(AppAdminConstants.SiteScriptsBuilderKey);
        scripts.AddScript($"csharpLanguageProvider.js", new ScriptFileInfo(new Uri("_content/MonacoRoslynCompletionProvider/monaco_csharp/csharpLanguageProvider.js", UriKind.Relative), placeInHead: true));

        scripts.AddScript($"registerCsharpProvider", new InlineBlockJavaScript("registerCsharpProvider()", placeInHead: false, order: 11));

        app.MapPost("/api/Monaco/completion/complete", async (TabCompletionRequest request) =>
        {
            var result = await CompletitionRequestHandler.Handle(request);
            return Results.Json(result);
        }).ShortCircuit();

        app.MapPost("/api/Monaco/completion/signature", async (SignatureHelpRequest request) =>
        {
            var result = await CompletitionRequestHandler.Handle(request);
            return Results.Json(result);
        }).ShortCircuit();

        app.MapPost("/api/Monaco/completion/hover", async (HoverInfoRequest request) =>
        {
            var result = await CompletitionRequestHandler.Handle(request);
            return Results.Json(result);
        }).ShortCircuit();

        app.MapPost("/api/Monaco/completion/codeCheck", async (CodeCheckRequest request) =>
        {
            var result = await CompletitionRequestHandler.Handle(request);
            return Results.Json(result);
        }).ShortCircuit();

        return app;
    }
}
