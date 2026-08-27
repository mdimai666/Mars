using Mars.Admin.Framework.Handlers.PostType;
using Mars.Contracts.PostTypes;

namespace Mars.Admin.Framework.Handlers;

public static class HandlersInstaller
{
    public static IServiceCollection InstallHandlers(this IServiceCollection services)
    {
        return services
            .AddScoped<IListModelHandler<PostTypeListItemResponse, TablePostTypeQueryRequest>, ListPostTypeHandler>();
    }
}
