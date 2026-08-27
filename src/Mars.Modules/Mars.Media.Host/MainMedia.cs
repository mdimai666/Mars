using Mars.Host.Handlers;
using Mars.Host.Services;
using Mars.Host.Services.GallerySpace;
using Mars.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Media.Host;

public static class MainMedia
{
    public static IServiceCollection AddMarsMedia(this IServiceCollection services)
    {
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IMediaFolderService, MediaFolderService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IGalleryService, GalleryService>();
        services.AddKeyedScoped<IMetaRelationModelProviderHandler, FileRelationModelProviderHandler>("File");
        return services;
    }
}
