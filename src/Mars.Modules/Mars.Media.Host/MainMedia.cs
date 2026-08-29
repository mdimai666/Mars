using System.Reflection;
using Mars.Cms.Abstractions.Services;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Services;
using Mars.Media.Contracts.Options;
using Mars.Media.Host.Handlers;
using Mars.Media.Host.Services;
using Mars.Media.Host.XActions;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.Validators;
using Mars.XActions.Abstractions.Managers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Media.Host;

public static class MainMedia
{
    public static IServiceCollection AddMarsMedia(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        ValidatorFactory.AddValidatorsFromAssembly(services, typeof(UploadMediaFileValidator).Assembly);

        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IMediaFolderService, MediaFolderService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddSingleton<IImageProcessor, ImageProcessor>();
        services.AddKeyedScoped<IMetaRelationModelProviderHandler, FileRelationModelProviderHandler>("File");

        services.AddMediaXActions();

        return services;
    }

    /// <summary>
    /// XActions медийной области: сканирование файлов и генерация миниатюр.
    /// </summary>
    public static IApplicationBuilder UseMarsMedia(this WebApplication app)
    {
        app.Services.GetRequiredService<IOptionService>().RegisterOption<MediaOption>();

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
