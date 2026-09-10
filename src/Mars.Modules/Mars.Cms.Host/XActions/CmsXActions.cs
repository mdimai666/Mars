using Mars.Cms.Host.XActions.ContentRecipes;
using Mars.XActions.Abstractions.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Cms.Host.XActions;

public static class CmsXActions
{
    public static IServiceCollection AddCmsXActions(this IServiceCollection services)
    {
        services.AddXActionHandlers(typeof(CreateMockPostsAct).Assembly);

        return services;
    }
}
