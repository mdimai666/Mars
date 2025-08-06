using Microsoft.Extensions.FileProviders;

namespace MarsDocs.DevServer.Startups;

public static class ServeMarkdownFilesExtensions
{
    public static void UseServeMarkdownFiles(this WebApplication app)
    {
        app.Map("/dev_docs", docsBuilder =>
        {
            // чтобы это работало в Blazor должно быть :nonfile - @page "/{*CatchAll:nonfile}"
            docsBuilder.UseStaticFiles(new StaticFileOptions
            {
                ServeUnknownFileTypes = true,
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "dev_docs")),
                DefaultContentType = "text/plain",
                OnPrepareResponse = ctx =>
                {
                    // Принудительно задать UTF-8 для текстовых файлов
                    if (ctx.File.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                        ctx.File.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                        ctx.Context.Response.ContentType.StartsWith("text/plain"))
                    {
                        ctx.Context.Response.ContentType = "text/plain; charset=utf-8";
                    }
                    ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                    ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                    ctx.Context.Response.Headers.Append("Expires", "0");
                }

            });
            // Завершаем пайплайн, если файл не найден — вернём 404
            docsBuilder.Run(async context =>
            {
                if (context.Response.HasStarted)
                {
                    return; // Файл уже найден и отправлен
                }
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"File not found in /dev_docs{context.Request.Path}");
            });
        });
    }
}
