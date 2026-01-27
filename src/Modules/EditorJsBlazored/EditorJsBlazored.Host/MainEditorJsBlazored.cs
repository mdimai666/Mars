using EditorJsBlazored.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EditorJsBlazored.Host;

public static class MainEditorJsBlazored
{
    public static IServiceCollection AddEditorJsBlazored(this IServiceCollection services)
    {
        services.AddHttpClient("EditorJsLinkTool", client =>
        {
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/120.0.0.0 Safari/537.36 " +
                "EditorJsLinkTool/1.0";

            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddSingleton<ILinkToolPreviewService, LinkToolPreviewService>();

        return services;
    }

    public static WebApplication UseEditorJsBlazored(this WebApplication app)
    {
        app.MapGet("/api/EditorJsBlazored/LinkTool",
            [Authorize] async (string url, ILinkToolPreviewService service, CancellationToken ct) =>
            {
                var result = await service.GetPreviewAsync(url, ct);

                return result != null
                    ? Results.Json(result)
                    : Results.Json(new { success = 0 });
            });

        return app;
    }
}
