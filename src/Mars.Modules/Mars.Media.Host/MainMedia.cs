using Mars.Cms.Abstractions.Services;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Services;
using Mars.Media.Host.Handlers;
using Mars.Media.Host.Services;
using Mars.Media.Host.Services.GallerySpace;
using Mars.Server.Abstractions.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Media.Host;

public static class MainMedia
{
    public static IServiceCollection AddMarsMedia(this IServiceCollection services)
    {
        ValidatorFactory.AddValidatorsFromAssembly(services, typeof(UploadMediaFileValidator).Assembly);

        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IMediaFolderService, MediaFolderService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IGalleryService, GalleryService>();
        services.AddKeyedScoped<IMetaRelationModelProviderHandler, FileRelationModelProviderHandler>("File");
        return services;
    }
}
