using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Repositories;

public static class MainRepositories
{
    public static IServiceCollection AddMarsHostRepositories(this IServiceCollection services)
        => services
            .AddScoped<IRoleRepository, RoleRepository>()
            .AddScoped<IUserRepository, UserRepository>()
            .AddScoped<IUserTypeRepository, UserTypeRepository>()
            .AddScoped<IPostTypeRepository, PostTypeRepository>()
            .AddScoped<IUserManager, UserManager__ReplacedToUserId>()
            .AddScoped<IOptionRepository, OptionRepository>()
            .AddScoped<INavMenuRepository, NavMenuRepository>()
            .AddScoped<IPostRepository, PostRepository>()
            .AddScoped<IFileRepository, FileRepository>()
            .AddScoped<IFeedbackRepository, FeedbackRepository>()
        ;
}
