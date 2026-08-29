using System.Reflection;
using Mars.Cms.Abstractions.Services;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Services;
using Mars.Media.Contracts.Options;
using Mars.Media.Host.Handlers;
using Mars.Media.Host.Services;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.Validators;
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

    public static IServiceProvider UseMarsMedia(this IServiceProvider services)
    {
        services.GetRequiredService<IOptionService>().RegisterOption<MediaOption>();
        return services;
    }
}
