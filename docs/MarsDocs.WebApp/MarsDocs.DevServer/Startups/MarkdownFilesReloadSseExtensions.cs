using MarsDocs.DevServer.Services;

namespace MarsDocs.DevServer.Startups;

public static class MarkdownFilesReloadSseExtensions
{
    public static void UseMarkdownFilesReloadSse(this WebApplication app)
    {
        app.MapGet("/sse", async (HttpContext context, SseManager sse) =>
        {
            context.Response.Headers.TryAdd("Content-Type", "text/event-stream");

            var client = new StreamWriter(context.Response.Body);
            sse.AddClient(client);

            try
            {
                while (!context.RequestAborted.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
            }
            finally
            {
                sse.RemoveClient(client);
            }
        });
    }
}
