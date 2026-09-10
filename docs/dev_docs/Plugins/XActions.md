# Создание XActions

XActions — универсальная командная шина платформы Mars: любое действие (очистить кеш, создать
шаблон, открыть страницу, исполнить поток) имеет уникальный id и может быть вызвано из админки
(кнопки, контекстные меню, палитра команд), из потоков Nodes, по HTTP API или ИИ-агентом.

Модель двухслойная (по аналогии с VS Code):

- **Act** — чистый хэндлер (`IAct`), исполнитель действия. Не знает, что он «команда».
- **XAction** — команда: метаданные (label, категория, видимость, схема аргументов) и привязка
  исполнения (`Handler<TAct>` или `Link`). Регистрируется императивно.

## Основные возможности XActions
- **Гибкость**: команду можно вызвать из разных частей платформы по одному id.
- **Расширяемость**: плагины регистрируют свои хэндлеры и команды в своём Startup.
- **Аргументы со схемой**: если у команды есть обязательные аргументы, админка сама покажет
  форму при вызове без аргументов; вызов с аргументами (поток, API) идёт сразу.

## Шаги для создания XAction

### 1. Реализация Act-хэндлера

Создайте класс, реализующий интерфейс `IAct`. Никаких атрибутов и статических объектов —
хэндлер это обычный DI-сервис:

```csharp
public class DeleteAllOrdersAct : IAct
{
    // id — строковая константа в формате owner.category.name (без nameof)
    public const string CommandId = "my_prefix.orders.deleteAllOrders";
    private readonly IService _service;

    public DeleteAllOrdersAct(IService service)
    {
        _service = service;
    }

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        // Логика выполнения действия
        await _service.DoWorkAsync(cancellationToken);
        return XActResult.ToastSuccess("Success");
    }
}
```

### 2. Регистрация хэндлеров из сборки плагина

В `ConfigureWebApplicationBuilder` (Startup.cs плагина) просканируйте сборку — все реализации
`IAct` будут зарегистрированы в DI:

```csharp
public override void ConfigureWebApplicationBuilder(WebApplicationBuilder builder, PluginSettings settings)
{
    builder.Services.AddXActionHandlers(typeof(MainXxxPlugin).Assembly);
}
```

### 3. Императивная регистрация команды

В `ConfigureWebApplication` зарегистрируйте команду со всеми настройками:

```csharp
public override void ConfigureWebApplication(WebApplication app, PluginSettings settings)
{
    var actionManager = app.Services.GetRequiredService<IActionManager>();

    actionManager.Add(a => a
        .Id(DeleteAllOrdersAct.CommandId)
        .Label("Delete all orders")
        .Category("Заказы")
        .FrontContexts("Mars.Admin.Pages.Settings.SettingsAboutSystemPage")
        .Handler<DeleteAllOrdersAct>());
}
```

Если у действия есть аргументы, объявите их схему — по ней админка сама нарисует форму вызова:

```csharp
actionManager.Add(a => a
    .Id(MyCustomAction.CommandId)
    .Label("My custom action")
    .Argument("postTypeName", "Тип записи", required: true)
    .Handler<MyCustomAction>());
```

В хэндлере аргументы доступны по имени: `context.Get("postTypeName")`.

### Варианты выбора (Choice)

У аргумента типа `Choice` варианты объявляются одним из двух способов:

- **Статические** — отдаются сразу вместе со схемой команды:

```csharp
actionManager.Add(a => a
    .Id(MyAction.CommandId)
    .Label("My action")
    .Argument("mode", "Режим", XActionArgumentType.Choice, options:
    [
        new() { Key = "fast", Label = "Быстрый" },
        new() { Key = "full", Label = "Полный" },
    ])
    .Handler<MyAction>());
```

- **Динамические** — указывается ключ источника, админка запросит варианты
  (`GET /api/Act/options/{ключ}`) перед отрисовкой формы:

```csharp
// при регистрации (ConfigureWebApplication)
actionManager.AddOptionsSource("orderStatuses", _ =>
    Task.FromResult<IReadOnlyCollection<XActionOption>>(
        GetStatuses().Select(s => new XActionOption { Key = s.Key, Label = s.Title }).ToList()));

// в команде
a.Argument("status", "Статус", XActionArgumentType.Choice, optionsSource: "orderStatuses")
```

В вызов передаётся `Key` варианта; `Label` — только отображение (локализация).

### 3.1. Регистрация простой ссылки (XLink)

Команда без хэндлера — навигация:

```csharp
actionManager.Add(a => a
    .Id("my_prefix.example.link")
    .Label("My link 1")
    .FrontContexts("Mars.Admin.Pages.Settings.SettingsAboutSystemPage")
    .Link("https://example.com"));
```

## Результат

`XActResult` — статус, сообщение (тост) и опциональные эффекты:

```csharp
return XActResult.ToastSuccess("готово")
    .WithNavigate("/admin/orders")      // перейти по URL
    .WithEvent("orders.changed")        // событие на клиентской шине
    .Then("my_prefix.orders.refresh");  // рекомендованное следующее действие
```

Эффекты — рекомендации, а не команды: автоцепочек нет, вызывающий (админка, поток Nodes)
сам решает, применять ли их. `NextAction` автоматически не выполняется никем.

## Вызов

- Из кода и потоков Nodes: `IActionManager.Inject(id, args, cancellationToken)`.
- По HTTP: `POST /api/Act/Inject`, тело `{ "id": "...", "args": { "имя": "значение" } }`.
- Из админки: кнопки, контекстные меню и палитра команд подхватывают зарегистрированные
  команды автоматически.
