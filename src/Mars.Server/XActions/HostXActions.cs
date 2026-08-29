using Mars.Contracts.XActions;
using Mars.Server.Abstractions.Managers;
using Mars.XActions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Server;

/// <summary>
/// Хостовые XActions: кеш, отладочные команды. Регистрируются без контекстов
/// админки — контексты навешивает оверлеем сторона, знающая админку (Mars.Admin.Host).
/// </summary>
public static class HostXActions
{
    public static IApplicationBuilder UseMarsHostXActions(this WebApplication app)
    {
        var actionManager = app.Services.GetRequiredService<IActionManager>();

        actionManager.Add(a =>
        {
            a.Id(ClearCacheAct.CommandId)
             .Label("Очистить кеш")
             .Category("Хост")
             .Recommended(10)
             .Handler<ClearCacheAct>();
        });

        actionManager.Add(a => a
            .Id("App.Logs")
            .Label("App logs")
            .Category("Разработка")
            .Link("/dev/builder/debug"));

#if DEBUG
        actionManager.Add(a =>
        {
            a.Id(DummyAct.CommandId)
             .Label("DummyAct")
             .Category("Отладка")
             .System()
             .Handler<DummyAct>();
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

        return app;
    }
}
