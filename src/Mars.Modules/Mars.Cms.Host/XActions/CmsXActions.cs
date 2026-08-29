using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Controllers;
using Mars.Cms.Host.XActions.ContentRecipes;
using Mars.Contracts.Resources;
using Mars.XActions.Abstractions.Managers;
using Mars.XActions.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Cms.Host.XActions;

/// <summary>
/// XActions контентной области. Команды регистрируются без привязки к страницам
/// админки — фронтовые контексты навешивает сторона, знающая админку (Mars.Admin.Host).
/// </summary>
public static class CmsXActions
{
    public static IServiceCollection AddCmsXActions(this IServiceCollection services)
    {
        services.AddXActionHandlers(typeof(CreateMockPostsAct).Assembly);

        return services;
    }

    public static IApplicationBuilder UseCmsXActions(this WebApplication app)
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
             .Label("Создать шаблон представления для типа записи")
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
            .Id("Mars.Admin.Posts.page.TemplateViewer")
            .Label("View static template")
            .Category("Контент")
            .Link("template/view"));

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
}
