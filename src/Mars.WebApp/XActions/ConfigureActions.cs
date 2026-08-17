using AppAdmin.Builder.DebugViews;
using AppAdmin.Builder.NodeViews;
#if !NOADMIN
using AppAdmin.Pages.FeedbackViews;
using AppAdmin.Pages.PostsViews;
using AppAdmin.Pages.PostTypeViews;
using AppAdmin.Pages.Settings;
#endif
using Mars.Controllers;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.XActions;
using Mars.Shared.Resources;
using Mars.XActions.ContentRecipes;

namespace Mars.XActions;

internal static class ConfigureActions
{

    public static IServiceCollection AddConfigureActions(this IServiceCollection services)
    {
        services.AddXActionHandlers(typeof(ClearCacheAct).Assembly);

        return services;
    }

    public static IApplicationBuilder UseConfigureActions(this WebApplication app)
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
            a.Id(ClearCacheAct.CommandId)
             .Label("Очистить кеш")
             .Category("Хост")
             .Recommended(10)
             .Handler<ClearCacheAct>();
#if !NOADMIN
            a.FrontContexts(typeof(SettingsHostCachePage).FullName!);
#endif
        });

        actionManager.Add(a =>
        {
            a.Id(CreateMockPostsAct.CommandId)
             .Label("Create mock posts")
             .Category("Контент")
             .Argument(CreateMockPostsAct.PostTypeArg, "Тип записи", XActionArgumentType.Choice, defaultValue: "post", optionsSource: CreateMockPostsAct.PostTypesOptionsSource)
             .Handler<CreateMockPostsAct>();
#if !NOADMIN
            a.FrontContexts(typeof(ManagePostPage).FullName + "-post");
#endif
        });

        actionManager.Add(a =>
        {
            a.Id(CreatePostTypePresentationTemplateAct.CommandId)
             .Label("Создать шаблон представления для типа записи")
             .Category("Контент")
             .Recommended(5)
             .Argument(CreatePostTypePresentationTemplateAct.PostTypeNameArg, "Тип записи", XActionArgumentType.Choice, required: true, optionsSource: CreateMockPostsAct.PostTypesOptionsSource)
             .Handler<CreatePostTypePresentationTemplateAct>();
#if !NOADMIN
            a.FrontContexts(typeof(EditPostTypePresentationPage).FullName!);
#endif
        });

#if DEBUG
        actionManager.Add(a =>
        {
            a.Id(DummyAct.CommandId)
             .Label("DummyAct")
             .Category("Отладка")
             .System()
             .Handler<DummyAct>();
#if !NOADMIN
            a.FrontContexts(typeof(SettingsPage).FullName!);
#endif
        });

        actionManager.Add(a => a
            .Id(FormTestAct.CommandId)
            .Label("Тест формы XAction")
            .Description("Строка, число, bool и выбор из списка — тост покажет введённое")
            .Category("Отладка")
            .Argument(FormTestAct.TextArg, "Строка", required: true)
            .Argument(FormTestAct.NumberArg, "Число", XActionArgumentType.Number, defaultValue: "42")
            .Argument(FormTestAct.BoolArg, "Флаг", XActionArgumentType.Bool, defaultValue: "true")
            .Argument(FormTestAct.ChoiceArg, "Выбор из списка", XActionArgumentType.Choice, options:
            [
                new() { Key = "one", Label = "Первый" },
                new() { Key = "two", Label = "Второй" },
                new() { Key = "three", Label = "Третий" },
            ])
            .Handler<FormTestAct>());

        actionManager.Add(a => a
            .Id(FrontDemoXAction.CommandId)
            .Label(FrontDemoXAction.Label)
            .Description("Исполняется на клиенте, хост такую команду не выполняет")
            .Category("Отладка")
            .FrontAction());
#endif

#if !NOADMIN
        actionManager.Add(a => a
            .Id("App.Logs")
            .Label("App logs")
            .Category("Разработка")
            .FrontContexts(typeof(SettingsAboutSystemPage).FullName!)
            .Link("/dev/builder/debug"));

        actionManager.Add(a => a
            .Id("AppAdmin.Posts.page.TemplateViewer")
            .Label("View static template")
            .Category("Контент")
            .FrontContexts(typeof(ManagePostPage).FullName! + "-page")
            .Link("template/view"));

#if DEBUG
        actionManager.Add(a => a
            .Id("Mars.Debug.EmptyLink")
            .Label("EmptyAct 1")
            .Category("Отладка")
            .FrontContexts(typeof(ManagePostPage).FullName + "-post")
            .Link("@empty"));

        actionManager.Add(a => a
            .Id(typeof(EditPostPage).FullName + "-page")
            .Label("test")
            .Category("Отладка")
            .FrontContexts(typeof(EditPostPage).FullName + "-page")
            .Link("/{page_slug}"));
#endif

        actionManager.Add(a => a
            .Id("Feedback.DownloadExcelList")
            .Label(AppRes.DownloadExcel)
            .Category("Обратная связь")
            .FrontContexts(typeof(FeedbackListPage).FullName!)
            .Link($"/api/Feedback/{nameof(FeedbackController.DownloadExcel)}"));
#endif

        actionManager.Add(a => a
            .Id(nameof(GenSourceCodeController.MetaTypesSourceCode) + "+csharp")
            .Label("Просмотр кода C#")
            .Category("Разработка")
            .FrontContexts(typeof(ListPostTypePage).FullName!)
            .Link($"/api/GenSourceCode/{nameof(GenSourceCodeController.MetaTypesSourceCode)}?lang=csharp"));

        actionManager.Add(a =>
        {
            a.Id(typeof(DebugPage).FullName!)
             .Label("Логи")
             .Category("Разработка")
             .Link("builder/debug");
#if !NOADMIN
            a.FrontContexts(typeof(SettingsPage).FullName!, typeof(NodeRedPage).FullName!);
#endif
        });

        actionManager.RefreshDict();

        return app;
    }

}
