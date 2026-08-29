using Mars.Media.Host.XActions;
using Mars.Server.Abstractions.Managers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Media.Host;

/// <summary>
/// XActions медийной области: сканирование файлов и генерация миниатюр.
/// </summary>
public static class MediaXActions
{
    public static IServiceCollection AddMediaXActions(this IServiceCollection services)
    {
        services.AddXActionHandlers(typeof(ScanMediaFilesAct).Assembly);

        return services;
    }

    public static IApplicationBuilder UseMediaXActions(this WebApplication app)
    {
        var actionManager = app.Services.GetRequiredService<IActionManager>();

        actionManager.Add(a =>
        {
            a.Id(ScanMediaFilesAct.CommandId)
             .Label("Сканировать файлы медиа")
             .Description("Находит файлы в хранилище, которых нет в базе, и регистрирует их")
             .Category("Медиа")
             .Handler<ScanMediaFilesAct>();
        });

        actionManager.Add(a =>
        {
            a.Id(GenerateThumbnailsAct.CommandId)
             .Label("Перегенерировать миниатюры")
             .Description("Пересоздаёт миниатюры и метаданные изображений")
             .Category("Медиа")
             .Handler<GenerateThumbnailsAct>();
        });

        return app;
    }
}
