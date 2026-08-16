# План добивки XActions — универсальной командной шины Mars

> **Статус: P1–P4 реализованы (2026-08-17).** Отклонения от плана зафиксированы в памяти
> проекта и git-истории: regex id допускает одиночный сегмент и `+`/`-`; `CustomEffect.CustomKind`
> (имя Kind конфликтует с дискриминатором `kind`); события эффектов уходят в клиентскую шину
> `Q.Root.Emit`; DEBUG-id `empty1` переименован в `Mars.Debug.EmptyLink`.
> Позже в тот же день: id команд переименованы в `mars.category.name` (строковые константы);
> селекторы переработаны — спецтипы `PostType`/`Front` и серверное обогащение удалены, вместо них
> варианты выбора `XActionOption { Key, Label }`: статические (в схеме) и динамические
> (`OptionsSource` → `GET /api/Act/options/{ключ}` перед отрисовкой формы), в вызов передаётся Key.

Задача: [FinalizeXActionsPrompt.md](./FinalizeXActionsPrompt.md).
Документ — дизайн и поэтапный план приведения XActions в вид «универсальный интерфейс всех действий
платформы» (вдохновение — Command Palette VS Code). Реализация — отдельными сессиями по фазам,
этот документ кодом не сопровождается.

## Принятые решения (2026-08-17)

1. **Два слоя, модель VS Code.** Act = чистый хэндлер `IAct`: локатор сканирует явно
   зарегистрированную сборку и регистрирует реализации в DI — они «просто существуют»
   (аналог handler-функций `registerCommand`). XAction = команда: регистрируется **императивно**
   в точке сборки модуля со всеми настройками и привязкой исполнения
   (`Handler<TAct>()` / `Link(url)` / провайдер с кастомным раннером). Аналог
   `contributes.commands` + `registerCommand` одним действием.
2. **Атрибут `RegisterXActionCommandAttribute` и статические свойства `XAction` удаляются** —
   корень текущей боли: декларация разорвана на два места, метаданные теряются, статика
   заводится только ради `FrontContextId`.
3. **Аргументы — именованные** (словарь имя→значение). Ломаем сейчас: потребителей ~5,
   плагины XActions ещё не используют.
4. **Формы — оба варианта**: по умолчанию генерик-модалка по декларативной схеме аргументов;
   реестр «actionId → кастомный Blazor-компонент» перекрывает генерик; вызов с аргументами —
   сразу, без формы (аналог Home Assistant services).
5. **Без автоцепочек** (ни серверных, ни фронтовых): результат несёт «рекомендованный следующий
   шаг» в виде эффектов; вызывающий сам решает, что делать (VS Code-style).
6. **AiChat-инструменты, хоткеи, переименование существующих id — вне скоупа** (см. конец).

---

## 1. Как устроено сейчас (as-is)

Все контракты — в одном файле `src/Mars.Shared/Contracts/XActions/XActionCommand.cs`:

- `XActionCommand` (record): `Id`, `Label`, `Type` (`XActionType.Link | HostAction`), `LinkValue`,
  `KeybindingContext`/`Keybindings` (**мёртвые** — никто не читает), `ContextMenuGroupId`/`ContextMenuOrder`,
  `FrontContextId[]` (видимость в контекстных меню по id страниц).
- `XActResult : IUserActionResult`: `Ok`, `Message`, `MessageIntent`, `NextStep`
  (`Toast | TriggerEvent | NextAction` — **работает только Toast**; `NextActionId` есть,
  аргументов и URL для остальных шагов нет), статические фабрики `ToastSuccess/Error/Warning/Info`.
- `XActionCommandCall { Id, string[] Args }` — тело HTTP-вызова.
- `IActContext { string[] args }`, `IAct.Execute(IActContext, CancellationToken)` — хэндлер.
- `RegisterXActionCommandAttribute(ActionId, Label)` — декларация команды на классе хэндлера
  (`src/Mars.Shared/Contracts/XActions/RegisterXActionCommandAttribute.cs`).

Реестр и исполнение:

- `src/Mars.Host/Managers/XActionManager.cs` — singleton `IActionManager`:
  `_registeredActions` (ручные `AddAction`/`AddXLink`) + `IXActionCommandsProvider`-ы →
  `RefreshDict()` сводит в `_allActions`; `Inject(id, args)` → `provider.RunCommand`.
- `src/Mars.Host/Managers/ActLocator.cs` — сканирует зарегистрированные сборки
  (`RegisterAssembly`) в поисках классов с атрибутом + `IAct`.
- `src/Mars.WebApp/XActions/ActActionsProvider.cs` — оборачивает локатор: `ReadCommands` строит
  `XActionCommand` **только из Id+Label атрибута** (остальные метаданные теряются),
  `RunCommand` создаёт инстанс через `ActivatorUtilities.CreateInstance` (мимо DI).
- `src/Mars.Nodes/Mars.Nodes.Host/Services/CommandNodesActionProvider.cs` — динамические команды
  из узлов `ActionCommandNode` (FrontContextId из ноды, раннер исполняет поток);
  подключается в `src/Mars.Nodes/Mars.Nodes.Host/MainMarsNodes.cs`.
- Контракты менеджера/провайдеров: `src/Mars.Host.Shared/Managers/IActionManager.cs`,
  `IXActionCommandsProvider.cs`, `IActActionsProvider.cs`.

Акты (все — в `src/Mars.WebApp/XActions/`): `ClearCacheAct.cs`, `DummyAct.cs` (DEBUG),
`ContentRecipes/CreateMockPostsAct.cs`, `ContentRecipes/CreatePostTypePresentationTemplateAct.cs`
(позиционный аргумент: ручная проверка `context.args.Length != 1`, `args[0]` = postTypeName).
У каждого — атрибут **и** статическое свойство `XAction` с дублем Id/Label ради `FrontContextId`.

Подключение и потребители:

- `src/Mars.WebApp/XActions/ConfigureActions.cs` — `RegisterAssembly` + ручной
  `AddAction(ClearCacheAct.XAction)` (дубль!) + пачка `AddXLink` на страницы админки.
- `src/Mars.WebApp/Controllers/ActController.cs` — `POST /api/Act/Inject/{actionId}`, тело `string[]`.
- `src/AppFront.Shared/Services/ActAppService.cs` — клиентский вызыватель: Link → навигация;
  HostAction → HTTP + тост по `Message`/`MessageIntent`; **понимает только Toast**.
- UI: `src/AppFront.Main/Components/XActionsDropDown.razor` (контекстный дропдаун, фильтр по
  `FrontContextId`), `src/AppAdmin/Shared/GActionCenterContainer.razor.cs` (палитра команд —
  глобальный поиск, исполнение XAction), `src/Mars.Host/Services/CentralSearchService.cs`
  (акции в результатах поиска). Словарь команд уезжает клиенту снапшотом при старте:
  `InitialSiteDataViewModel.XActions` (`src/Mars.WebApp/Endpoints/InitialSiteDataEndpoint.cs`).
- Nodes: `src/Mars.Nodes/Mars.Nodes.Core.Implements/Nodes/Connections/ExecXActionNodeImpl.cs` —
  вызывает `Inject(Node.CommandId, [])` (аргументы не передаются), кладёт результат в payload.

### Проблемы

1. **Разорванная декларация**: атрибут несёт Id+Label, провайдер теряет остальное → статические
   объекты `XAction` + ручной `AddAction` в `ConfigureActions` (двойная регистрация).
2. **Неявная «авторегистрация»**: любой класс с атрибутом в зарегистрированной сборке становится
   командой, но с неполными метаданными; точка, где видно «какие команды есть и с какими
   настройками», отсутствует.
3. **Нет схемы аргументов и форм**: позиционный `string[]`, ручные проверки в актах.
4. **`NextStep.TriggerEvent`/`NextAction` и `Keybindings` объявлены, но не работают.**
5. **Акты инстанцируются `ActivatorUtilities`-ом мимо DI** (scoped-зависимости живут на запрос
   неправильно, тестировать сложнее).
6. **Словарь на фронте застаёт**: Nodes деплоит команды в рантайме (`RefreshDict`), а
   `Q.Site.XActions` — снапшот старта страницы.
7. Нет различия «системная/пользовательская», нет категорий, id не валидируются
   (встречаются `empty1`, `typeof(...).FullName`).
8. Нет интеграции с AiChat (в этой итерации не делаем, но модель не должна мешать).

---

## 2. Общепринятые практики — что у кого берём

### VS Code (прямой источник вдохновения; ответ на вопрос «как это сделано там?»)

- Команда существует в **двух слоях**: декларативный манифест в `package.json`
  (`contributes.commands`: `command`/`title`/`category`/`icon`/`enablement`) — описание для UI;
  и императивная регистрация хэндлера в `activate()` расширения:
  `vscode.commands.registerCommand(id, fn, thisArg)` → возвращает Disposable для отмены.
- **Статических объектов нет**: реестр (`ICommandService`) живёт в платформе; расширение лишь
  складывает в него пары id→fn. **Ничего не регистрирует само себя** — расширение явно решает,
  какие команды регистрировать при активации.
- Видимость/доступность — `when`-контексты в местах использования (меню/палитра/хоткеи —
  отдельные contribution points, ссылающиеся на id команды).
- Команда может быть без хэндлера («command not found») и хэндлер может быть без манифеста
  (headless — вызывается кодом, не виден в палитре). Это готовая модель «системных» действий.
- `executeCommand(id, ...args)` возвращает значение; **цепочки строит вызывающий**, не платформа.

Берём: два слоя (хэндлер ≠ команда), императивная точка регистрации на модуль, видимость через
контексты, результат возвращается вызывающему.

### JetBrains IDE Actions

`plugin.xml` декларирует `<action id="..." class="..." text="..." description="..."/>`,
класс наследует `AnAction`, платформу инстанцирует сама; группы/меню — отдельные декларации.
Подтверждает: метаданные команды и класс-исполнитель — разные сущности, инстанцирует платформа.

### Home Assistant services

Сервис = id + декларативная схема аргументов (services.yaml, selectors). UI **сам рисует форму**
по схеме; автоматизации/агенты вызывают сервис с готовыми данными — без формы. Selectors с
динамическими вариантами (списки сущностей) резолвятся сервером при отдаче схемы.
Берём: схема аргументов в метаданных + автоформа + серверная подгрузка вариантов выбора.

### MediatR / CQRS и WPF

MediatR: request/handler — отдельные типы в DI, единая точка dispatch,
`RegisterServicesFromAssembly(assembly)` — **явная регистрация сборки, обход внутри неявный**.
Это и есть наш аналог VS Code-`activate()`: модуль явно говорит «handlers в этой сборке»,
локатор находит их сам. (WPF MVVM Toolkit `[RelayCommand]` — пример атрибутной генерации команд;
сознательно не берём: императивная регистрация явнее и ближе к VS Code.)

---

## 3. Назначение (фиксация)

**XActions — единая командная шина платформы.** Любое действие (очистить кеш, создать шаблон,
открыть страницу, исполнить поток, позже — запустить ИИ-сценарий) имеет:

- уникальный id (конвенция `Owner.Module.Name`);
- декларативные метаданные (label, категория, описание, иконка, видимость, схема аргументов);
- стратегию исполнения (хэндлер-Act / ссылка / кастомный раннер провайдера).

Вызывается **откуда угодно**: кнопка и контекстное меню в админке, палитра команд, потоки Nodes,
HTTP API, в перспективе — ИИ-агент. Одна регистрация — все потребители.

Разделение ролей:

- **Act** — чистый исполнитель (`IAct`). Не знает, что он «команда»: никаких атрибутов и
  метаданных. Один Act может стоять за несколькими XActions (разные настройки/дефолтные аргументы).
- **XAction** — команда: метаданные + привязка исполнения. Регистрируется императивно.
- **XLink** — частный случай команды без хэндлера (навигация); не «костыль», а полноценная
  декларативная команда (в VS Code таких много).

---

## 4. Целевая модель

### 4.1. Хэндлеры: сканирование сборки → DI

```csharp
// в UseConfigureActions / Use*-методе модуля / bootstrap плагина:
services.AddXActionHandlers(typeof(ClearCacheAct).Assembly);
```

- Локатор (`ActLocator`) ищет **все реализации `IAct`** в сборке — атрибут больше не нужен.
- Каждая регистрируется в DI как scoped (`services.AddScoped(actType)`); исполнение резолвится
  из контейнера. Fallback `ActivatorUtilities` — только для плагинов, не добавивших хэндлеры в DI.
- Хэндлеры без зарегистрированной команды «просто существуют» — headless, как в VS Code.

### 4.2. Команды: императивная регистрация со всеми настройками

```csharp
var xactions = services.AddXActions();

xactions.Add(a => a
    .Id("Mars.XActions.ClearCache")
    .Label("Очистить кеш")
    .Category("Хост")
    .Description("Сбрасывает кеш приложения")
    .FrontContexts([typeof(SettingsPage).FullName!])
    .Handler<ClearCacheAct>());

xactions.AddLink("App.Posts", "/admin/posts", label: "Записи",
    frontContexts: [typeof(ManagePostPage).FullName!]);

xactions.AddProvider(commandNodesActionProvider); // Nodes: команды-потоки с кастомным раннером
```

Точки регистрации: `src/Mars.WebApp/XActions/ConfigureActions.cs` (WebApp-акты и линки),
`MainMarsNodes` (провайдер Nodes), bootstrap плагинов (P4). Здесь же видно весь список команд
модуля с настройками — аналог `activate()` + `contributes` в одном месте.

### 4.3. Модель метаданных

```csharp
public record XActionCommand
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public string? Category { get; init; }        // группировка в палитре
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public bool System { get; init; }             // скрыт из палитры/меню; вызов кодом/потоками/API
    public string[]? FrontContextId { get; init; }

    public XActionType Type { get; init; }        // Link | HostAction — производное от привязки
    public string? LinkValue { get; init; }       // для Link

    public XActionArgument[]? Arguments { get; init; }
}

public record XActionArgument
{
    public required string Name { get; init; }
    public required string Label { get; init; }
    public XActionArgumentType Type { get; init; } // String | Number | Bool | Choice | PostType | Front
    public bool Required { get; init; }
    public string? DefaultValue { get; init; }
    public string[]? Choices { get; init; }        // для Choice; PostType/Front обогащаются сервером
}
```

- `KeybindingContext`/`Keybindings` удаляются (мёртвые; хоткеи — отдельная будущая тема).
- `ContextMenuGroupId`/`ContextMenuOrder` — оставить, пока дропдаун их использует (P4 ревизует).
- `XActionType` сохраняется как сериализуемое производное (фронту нужно отличать Link от
  HostAction), но задаётся привязкой исполнения, а не руками.

### 4.4. Аргументы и вызов

```csharp
public interface IActContext
{
    IReadOnlyDictionary<string, string> Args { get; }
    string? Get(string name);                        // + GetInt/GetBool-хелперы
}

public record XActionCommandCall
{
    public required string Id { get; init; }
    public IReadOnlyDictionary<string, string> Args { get; init; } = {};
}
```

- `IActionManager.Inject(id, args, ct)` — сигнатура та же, `args` становится словарём.
- HTTP: `POST /api/Act/Inject` с телом `XActionCommandCall` (id в теле, не в маршруте) —
  единственный потребитель (фронт) переделывается в той же фазе.
- Валидация **до исполнения**: неизвестный id → «command not found»; отсутствующий required-
  аргумент → ошибка результата, а не ручные проверки в актах.
- Миграция актов: `CreatePostTypePresentationTemplateAct` → `Args["postTypeName"]`
  (+ схема в регистрации команды).

### 4.5. Результат и эффекты (без автоцепочек)

```csharp
public class XActResult : IUserActionResult
{
    public bool Ok { get; init; }
    public string? Message { get; init; }             // что сказать (тост)
    public MessageIntent MessageIntent { get; init; }
    public IReadOnlyList<XActionEffect> Effects { get; init; } = [];  // что рекомендовано дальше
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(NavigateEffect),     "navigate")]
[JsonDerivedType(typeof(NextActionEffect),   "nextAction")]
[JsonDerivedType(typeof(TriggerEventEffect), "event")]
[JsonDerivedType(typeof(CustomEffect),       "custom")]
public abstract record XActionEffect;

public sealed record NavigateEffect(string Url) : XActionEffect;
public sealed record NextActionEffect(string ActionId,
    IReadOnlyDictionary<string, string>? Args = null) : XActionEffect;
public sealed record TriggerEventEffect(string Name, JsonElement? Payload = null) : XActionEffect;
public sealed record CustomEffect(string CustomKind, JsonElement Data) : XActionEffect; // люк плагинов (Kind занят дискриминатором)
```

- Эффекты — **рекомендации**, не команды. Неизвестные `kind` игнорируются (форвард-совместимость).
- Старые `NextStep`/`NextActionId` удаляются; фабрики `Toast*` остаются, добавляются
  билдеры `.WithNavigate(url)`, `.WithEvent(name, payload)`, `.Then(actionId, args)`.
- **Контракт интерпретации**: `ActAppService` (стандартный UI-вызыватель) применяет только
  «отрисовку результата»: `Message` → тост, `NavigateEffect` → навигация,
  `TriggerEventEffect` → клиентская шина `Q.Root.Emit`. `NextActionEffect`/`CustomEffect` **не исполняются
  автоматически никем** — возвращаются вызывающему (компонент кнопки, узел потока).
  Автоцепочка (автозапуск nextAction) не реализуется нигде — ни на сервере, ни на фронте.
- Расширяемость: новый сценарий = новый record + `[JsonDerivedType]` + обработчик.
  Будущий AiChat-сценарий («создать политику конфиденциальности» → ИИ-чат с промптом) —
  это будущий `AiChatEffect(Prompt, Mode)`; текущая модель его не блокирует.
  Плагины не могут регистрировать `JsonDerivedType` в рантайме — для них `CustomEffect`.
- `ExecXActionNodeImpl`: результат целиком (включая эффекты) уходит в payload узла — цепочки
  строятся средствами потока, не платформой.

### 4.6. Формы аргументов

Порядок разрешения при вызове из UI (`ActAppService`):

1. Аргументы уже переданы (кнопка с фиксированными значениями, поток, API) → сразу `Inject`.
2. Для id зарегистрирована кастомная форма → показать её.
3. В схеме есть `Required` (или любые аргументы) → генерик-модалка по схеме → `Inject` с вводом.
4. Иначе → сразу `Inject`.

- **Генерик-форма** рендерится по `Arguments`: `String`/`Number`/`Bool`/`Choice` — поля и
  select; `PostType`/`Front` (селекторы) — select с вариантами, которые сервер подкладывает в
  метаданные при отдаче списка команд (аналог Home Assistant selectors).
- **Кастомные формы**: фронтовый реестр `IXActionFormProvider` (actionId → Blazor-компонент),
  регистрация в AppAdmin; перекрывает генерик. Сервер о кастомных формах не знает — метаданных
  схемы достаточно, чтобы фронт сам решил, спрашивать ли.
- Сами XActions от форм не усложняются: схема — данные, отрисовка — ответственность фронта.

### 4.7. Id и валидация

- Конвенция `Owner.Module.Name`, формат `^[A-Za-z][A-Za-z0-9_+\-]*(\.[A-Za-z][A-Za-z0-9_+\-]*)*$`
  (сегменты могут содержать `+`/`-` — такие id уже есть; одиночный сегмент допустим ради
  легаси-id), проверка при регистрации вместе с уникальностью и наличием привязки исполнения.
- **Существующие id не переименовываем**: они сохранены в потоках (`ExecXActionNode.CommandId`)
  и в разметке страниц. Нормализация — только для новых команд.

---

## 5. Фазы внедрения

### P1 — Ядро: двухслойная модель, DI, императивная регистрация

- `Mars.Shared/Contracts/XActions/XActionCommand.cs`: модель 4.3–4.4 (убрать Keybindings,
  добавить Category/Description/Icon/System/Arguments; `IActContext`/`XActionCommandCall` →
  словарь); удалить `RegisterXActionCommandAttribute.cs`.
- `Mars.Host/Managers/ActLocator.cs`: скан `IAct` без атрибута; DI-регистрация хэндлеров;
  резолв из контейнера (fallback `ActivatorUtilities`).
- `Mars.Host/Managers/XActionManager.cs` + `Mars.Host.Shared/Managers/*`: императивный API
  регистрации (`Add`/`AddLink`/`AddProvider`, fluent-билдер), валидация, `Inject` со словарём.
- `Mars.WebApp/XActions/*`: `ConfigureActions` — единый императивный список команд и линков;
  акты теряют атрибуты и статические `XAction` (4 класса); `CreatePostTypePresentationTemplateAct`
  — именованный аргумент + схема; `ActActionsProvider` — либо удаляется в пользу реестра,
  либо становится тонким провайдером отсканированных хэндлеров (решить при реализации).
- `Mars.WebApp/Controllers/ActController.cs`: контракт 4.4.
- `src/Mars.WebApiClient/Implements/ActServiceClient.cs` + `Interfaces/IActServiceClient.cs`:
  миграция `Inject` на новый контракт (словарь аргументов, id в теле). Существующих тестов,
  использующих клиент, нет — только новые.
- `ExecXActionNodeImpl.cs`: адаптация к новому `IActContext`.

### P2 — Результат и эффекты

- `XActResult` + union `XActionEffect` (4.5); удалить `NextStep`/`NextActionId`.
- `src/AppFront.Shared/Services/ActAppService.cs`: интерпретация (тост/navigate/событие в
  `FrontEventHub`), возврат результата вызывающему; без автозапуска nextAction.
- `ExecXActionNodeImpl`: эффекты в payload.

### P3 — Формы

- Отдача схемы фронту: `InitialSiteDataViewModel.XActions` + новый `GET /api/Act/list`
  (с серверным обогащением селекторов PostType/Front/Choice).
- Генерик-модалка по схеме (AppAdmin/AppFront, FluentUI — по существующим компонентам админки).
- `IXActionFormProvider` (фронтовый реестр actionId → компонент) + логика разрешения 4.6 в
  `ActAppService`.

### P4 — UI и экосистема

- Палитра (`src/AppAdmin/Shared/GActionCenterContainer.razor.cs`,
  `src/Mars.Host/Services/CentralSearchService.cs`): группировка по `Category`, фильтр
  `System`, **живой список через `GET /api/Act/list` при открытии** вместо стареющего снапшота
  `Q.Site.XActions` (альтернатива — SignalR-инвалидация; API проще и достаточен).
- `XActionsDropDown.razor`: фильтр `System`, ревизия ContextMenu-полей.
- `ExecXActionNode`: входы для аргументов (проброс в `Inject`).
- Плагины: проверить lifecycle-хук bootstrap-а плагинов и дать им точку
  `AddXActionHandlers(pluginAssembly)` + регистрацию команд; дополнить
  `ai/PluginCreationGuide.md` разделом про XActions.

---

## 6. Верификация

Точечно — **без прогонов всего набора тестов**:

- `dotnet build Mars.slnx` — сборка.
- Новые интеграционные тесты через `IActServiceClient`
  (`src/Mars.WebApiClient/Implements/ActServiceClient.cs`):
  `Inject` с именованными аргументами (`CreatePostTypePresentationTemplateAct`), ошибка на
  отсутствующий required-аргумент, неизвестный id → «command not found», линк-команда.
- Новые тесты только на создаваемые эндпоинты (P3: `GET /api/Act/list` — схема аргументов,
  обогащение селекторов, фильтр `System`).
- UI фаз P3/P4 (генерик-форма, палитра) — визуально при разработке фазы, отдельным прогоном
  не проверяется.

## Вне скоупа (зафиксировано)

- Инструменты AiChat для XActions (модель эффектов их не блокирует — будущий `AiChatEffect`).
- Хоткеи/keybindings (мёртвые поля удаляются, тема отдельная).
- Автоцепочки nextAction — серверные и фронтовые.
- Переименование существующих id команд.
