# Создание XActions

XActions — это расширяемый механизм для выполнения действий в рамках плагинов платформы Mars. Они позволяют разработчикам определять и выполнять действия, которые могут быть интегрированы в различные модули системы.

## Основные возможности XActions
- **Гибкость**: Позволяют создавать действия, которые могут быть вызваны из различных частей приложения.
- **Расширяемость**: Легко интегрируются с существующими плагинами.

## Шаги для создания XAction

### 1. Определение интерфейса действия
Создайте интерфейс, который будет описывать контракт вашего действия. Например:
```
public interface IAct
{
    Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken);
}
```

### 2. Реализация XAction
Создайте класс, который реализует интерфейс `IAct`. Например:

```
[RegisterXActionCommand(CommandId, "Delete all orders")]
public class MyCustomAction : IAct
{
    public const string CommandId = "my_prefix." + nameof(MyCustomAction);
    private readonly IService _service;

    public MyCustomAction(IService service)
    {
        _service = service;
    }

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken);
    {
        // Логика выполнения действия
        await _service.DoWorkAsync(cancellationToken);
        return XActResult.ToastSuccess("Success");
    }
}
```

### 2. Регистрация XAction

```
var actActionsProvider = serviceProvider.GetRequiredService<IActActionsProvider>();
actActionsProvider.RegisterAssembly(typeof(CreateMockProductsAct).Assembly);

actionManager.AddAction(new XActionCommand
    {
        Id = MyCustomAction.CommandId,
        Label = "MyCustomAction",
        Type = XActionType.HostAction,
        FrontContextId = ["AppAdmin.Pages.Settings.SettingsAboutSystemPage"]
    });
```
### 2.1. Регистрация простой ссылки XAction
```
actionManager.AddXLink(new XActionCommand
{
    Id = "my_action_id",
    LinkValue = "https://example.com",
    Label = "My link 1",
    Type = XActionType.Link,
    FrontContextId = ["AppAdmin.Pages.Settings.SettingsAboutSystemPage"]
});
```
