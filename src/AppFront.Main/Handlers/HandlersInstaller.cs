using AppFront.Shared.Handlers.PostType;
using Mars.Shared.Contracts.PostTypes;

namespace AppFront.Shared.Handlers;

public static class HandlersInstaller
{
    public static IServiceCollection InstallHandlers(this IServiceCollection services)
    {
        return services
            .AddScoped<IListModelHandler<PostTypeListItemResponse, TablePostTypeQueryRequest>, ListPostTypeHandler>();
    }
}
