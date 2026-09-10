using System.Reflection;
using Mars.Cms.Abstractions.Attributes;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Host.Controllers;
using Mars.Cms.Host.Handlers;
using Mars.Cms.Host.Seeding;
using Mars.Cms.Host.Services;
using Mars.Cms.Host.Services.GallerySpace;
using Mars.Cms.Host.XActions;
using Mars.Cms.Host.XActions.ContentRecipes;
using Mars.Contracts.Resources;
using Mars.Data.Seeding;
using Mars.Server.Abstractions.Validators;
using Mars.XActions.Abstractions.Managers;
using Mars.XActions.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Cms.Host;

public static class MainCms
{
    public static IServiceCollection AddMarsCms(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        ValidatorFactory.AddValidatorsFromAssembly(services, typeof(CreatePostQueryValidator).Assembly);

        services.AddSingleton<INavMenuService, NavMenuService>();
        services.AddSingleton<IMetaModelTypesLocator, MetaModelTypesLocator>();
        services.AddSingleton<IPostCategoryMetaLocator, PostCategoryMetaLocator>();

        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IPostTypeService, PostTypeService>();
        services.AddScoped<IPostJsonService, PostJsonService>();
        services.AddScoped<IPostCategoryService, PostCategoryService>();
        services.AddScoped<IPostCategoryTypeService, PostCategoryTypeService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IGalleryService, GalleryService>();

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
        services.AddSingleton<ISeedDataHandler, CmsSeedDataHandler>();
        services.AddScoped<ICentralSearchService, CentralSearchService>();
        services.AddScoped<ICentralSearchProvider, PostTypesSearchProvider>();
        services.AddScoped<ICentralSearchProvider, PostsSearchProvider>();
        services.AddSingleton<IDatabaseEntityTypeCatalogService, DatabaseEntityTypeCatalogService>();

        UseIMetaRelationModelProviderHandler(services);
        UseIMetaValueGeneratorHandler(services);
        services.AddScoped<IPostTransformer, PostTransformer>();
        RegisterPostContentProcessorsLocator(services);

        services.AddCmsXActions();

        return services;
    }

    /// <summary>
    /// XActions контентной области. Команды регистрируются без привязки к страницам
    /// админки — фронтовые контексты навешивает сторона, знающая админку (Mars.Admin.Host).
    /// </summary>
    public static IApplicationBuilder UseMarsCms(this WebApplication app)
    {
        var actionManager = app.Services.GetRequiredService<IActionManager>();

        // динамический источник вариантов «типы записей» — фронт запрашивает его
        // перед отрисовкой форм, где аргумент объявлен с optionsSource
        var metaModelTypesLocator = app.Services.GetRequiredService<IMetaModelTypesLocator>();
        actionManager.AddOptionsSource(CreateMockPostsAct.PostTypesOptionsSource, _ =>
            Task.FromResult<IReadOnlyCollection<XActionOption>>(
                metaModelTypesLocator.PostTypesDict().Keys
                    .Select(k => new XActionOption { Key = k, Label = k })
                    .ToList()));

        actionManager.Add(a =>
        {
            a.Id(CreateMockPostsAct.CommandId)
             .Label("Create mock posts")
             .Category("Контент")
             .Argument(CreateMockPostsAct.PostTypeArg, "Тип записи", XActionArgumentType.Choice, defaultValue: "post", optionsSource: CreateMockPostsAct.PostTypesOptionsSource)
             .Handler<CreateMockPostsAct>();
        });

        actionManager.Add(a =>
        {
            a.Id(CreatePostTypePresentationTemplateAct.CommandId)
             .Label("Создать шаблон представления для типа записей")
             .Category("Контент")
             .Recommended(5)
             .Argument(CreatePostTypePresentationTemplateAct.PostTypeNameArg, "Тип записи", XActionArgumentType.Choice, required: true, optionsSource: CreateMockPostsAct.PostTypesOptionsSource)
             .Handler<CreatePostTypePresentationTemplateAct>();
        });

        actionManager.Add(a =>
        {
            a.Id(RegenerateGeneratedMetaValuesAct.CommandId)
             .Label("Перегенерировать значения полей-генераторов")
             .Description("Порядковые номера и даты у существующих постов: перенумеровать, только сегодня или дозаполнить пустые")
             .Category("Контент")
             .Argument(RegenerateGeneratedMetaValuesAct.PostTypeArg, "Тип записи", XActionArgumentType.Choice, required: true, optionsSource: CreateMockPostsAct.PostTypesOptionsSource)
             .Argument(RegenerateGeneratedMetaValuesAct.ModeArg, "Режим", XActionArgumentType.Choice, defaultValue: RegenerateGeneratedMetaValuesAct.ModeAll, options:
             [
                 new() { Key = RegenerateGeneratedMetaValuesAct.ModeAll, Label = "Перенумеровать с первого" },
                 new() { Key = RegenerateGeneratedMetaValuesAct.ModeToday, Label = "Перенумеровать за сегодня" },
                 new() { Key = RegenerateGeneratedMetaValuesAct.ModeFromLast, Label = "Дозаполнить пустые (продолжить)" },
             ])
             .Argument(RegenerateGeneratedMetaValuesAct.StatusesArg, "Статусы (slug через запятую; пусто — все)")
             .Handler<RegenerateGeneratedMetaValuesAct>();
        });

        actionManager.Add(a => a
            .Id("Feedback.DownloadExcelList")
            .Label(AppRes.DownloadExcel)
            .Category("Обратная связь")
            .Link($"/api/Feedback/{nameof(FeedbackController.DownloadExcel)}"));

        actionManager.Add(a => a
            .Id(nameof(GenSourceCodeController.MetaTypesSourceCode) + "+csharp")
            .Label("Просмотр кода C#")
            .Category("Разработка")
            .Link($"/api/GenSourceCode/{nameof(GenSourceCodeController.MetaTypesSourceCode)}?lang=csharp"));

        return app;
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
