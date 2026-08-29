using Mars.Cms.Abstractions.Repositories;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Services;
using Mars.Media.Abstractions.Repositories;
using Mars.Options.Abstractions.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Data.Repositories;

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
            .AddScoped<IMediaFolderRepository, MediaFolderRepository>()
            .AddScoped<IFeedbackRepository, FeedbackRepository>()
            .AddScoped<IPostCategoryRepository, PostCategoryRepository>()
            .AddScoped<IPostCategoryTypeRepository, PostCategoryTypeRepository>()
            .AddScoped<IMetaSequenceRepository, MetaSequenceRepository>()
        ;
}
