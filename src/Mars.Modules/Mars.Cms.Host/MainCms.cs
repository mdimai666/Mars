using System.Reflection;
using Mars.Host.Handlers;
using Mars.Host.Services;
using Mars.Host.Shared.Attributes;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.MetaFields;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Cms.Host;

public static class MainCms
{
    public static IServiceCollection AddMarsCms(this IServiceCollection services)
    {
        services.AddSingleton<INavMenuService, NavMenuService>();
        services.AddSingleton<IMetaModelTypesLocator, MetaModelTypesLocator>();
        services.AddSingleton<IPostCategoryMetaLocator, PostCategoryMetaLocator>();

        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IPostTypeService, PostTypeService>();
        services.AddScoped<IPostJsonService, PostJsonService>();
        services.AddScoped<IPostCategoryService, PostCategoryService>();
        services.AddScoped<IPostCategoryTypeService, PostCategoryTypeService>();
        services.AddScoped<IFeedbackService, FeedbackService>();

        services.AddScoped<IMetaFieldMaterializerService, MetaFieldMaterializerService>();
        services.AddScoped<IMetaQueryFieldResolver, MetaQueryFieldResolver>();
        services.AddScoped<IMtoRelationMaterializer, MtoRelationMaterializer>();
        services.AddScoped<IPostTypeViewService, PostTypeViewService>();
        services.AddScoped<IPostMetaColumnsService, PostMetaColumnsService>();
        services.AddScoped<IMetaValuesValidator, MetaValuesValidator>();
        services.AddKeyedScoped<IMetaValueUniquenessProvider, PostMetaValueUniquenessProvider>(MetaValueOwnerCatalog.Post)
                .AddKeyedScoped<IMetaValueUniquenessProvider, PostCategoryMetaValueUniquenessProvider>(MetaValueOwnerCatalog.PostCategory)
                .AddKeyedScoped<IMetaValueUniquenessProvider, UserMetaValueUniquenessProvider>(MetaValueOwnerCatalog.User);
        services.AddScoped<IMetaValuesGeneratorService, MetaValuesGeneratorService>();
        services.AddScoped<ICentralSearchService, CentralSearchService>();
        services.AddScoped<ICentralSearchProvider, PostTypesSearchProvider>();
        services.AddScoped<ICentralSearchProvider, PostsSearchProvider>();
        services.AddSingleton<IDatabaseEntityTypeCatalogService, DatabaseEntityTypeCatalogService>();

        UseIMetaRelationModelProviderHandler(services);
        UseIMetaValueGeneratorHandler(services);
        services.AddScoped<IPostTransformer, PostTransformer>();
        RegisterPostContentProcessorsLocator(services);

        return services;
    }

    static void UseIMetaRelationModelProviderHandler(IServiceCollection services)
    {
        services
            .AddKeyedScoped<IMetaRelationModelProviderHandler, UserRelationModelProviderHandler>("User")
            .AddKeyedScoped<IMetaRelationModelProviderHandler, PostRelationModelProviderHandler>("Post")
            .AddKeyedScoped<IMetaRelationModelProviderHandler, FeedbackRelationModelProviderHandler>("Feedback")
            .AddKeyedScoped<IMetaRelationModelProviderHandler, NavMenuRelationModelProviderHandler>("NavMenu")
            ;
    }

    static void UseIMetaValueGeneratorHandler(IServiceCollection services)
    {
        services
            .AddKeyedScoped<IMetaValueGeneratorHandler, SequenceValueGeneratorHandler>(MetaFieldGeneratorCatalog.Sequence)
            .AddKeyedScoped<IMetaValueGeneratorHandler, NowValueGeneratorHandler>(MetaFieldGeneratorCatalog.Now)
            ;
    }

    static IServiceCollection RegisterPostContentProcessorsLocator(IServiceCollection services)
    {
        services.AddSingleton<IPostContentProcessorsLocator, PostContentProcessorsLocator>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location));

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (!type.IsClass || type.IsAbstract)
                continue;

            var attr = type.GetCustomAttribute<KeyredHandlerAttribute>();
            if (attr == null)
                continue;

            if (typeof(IPostContentProcessor).IsAssignableFrom(type))
            {
                var key = attr.Key ?? type.Name;
                services.AddKeyedScoped(typeof(IPostContentProcessor), key, type);
            }
        }

        return services;
    }
}
