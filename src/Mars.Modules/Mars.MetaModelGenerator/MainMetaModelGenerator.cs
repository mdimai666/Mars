using Mars.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.MetaModelGenerator;

public static class MainMetaModelGenerator
{
    public static IServiceCollection AddMetaModelGenerator(this IServiceCollection services)
        => services.AddSingleton<IMetaEntityTypeProvider, MetaEntityTypeProvider>();
}
