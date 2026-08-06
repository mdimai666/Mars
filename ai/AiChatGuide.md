# Mars AiChat Guide for Agent

## Обзор

**Mars.AiChat** — встроенный ИИ-агент админ-панели Mars в стиле терминала (вдохновлён Qwen Code CLI).
Администратор ставит задачи текстом («поменяй имя сайта», «создай пост о …»), агент выполняет их
через инструменты (function tools) в harness-цикле Microsoft Agent Framework.

UI: плавающая кнопка «ИИ агент» внизу экрана (перетаскивается, прилипает к краям) и терминальное
модальное окно внизу по центру: строка ввода `❯` + Enter, стриминг ответа, строки вызовов инструментов,
кнопки «стоп» / «новый чат» / переключение чатов. Агент умеет задавать уточняющие вопросы пользователю.

Модуль включается feature-флагом `AiChat` (`appsettings.json` → `FeatureManagement`).

## Архитектура

### Проекты (`src/Mars.Modules`)

| Проект | Назначение |
|---|---|
| `Mars.AiChat.Shared` | Чистые DTO/опции, общие для фронта и хоста: `AiChatOption`, `AiProviderConnection`, `AiChatMessageDto`, события `AiChatHubEvents` |
| `Mars.AiChat.Host.Shared` | Серверные интерфейсы (`IAiChatSessionStore`, `IAiChatClientFactory`, `IAiChatRunCoordinator`) и модель `AiChatSessionState` — для внешнего переиспользования |
| `Mars.AiChat.Host` | Бэкенд: контроллер, SignalR-хаб, координатор запусков, harness-сервис агента, инструменты |
| `Mars.AiChat.Front` | Blazor RCL: контейнер в `App.razor`, терминал, форма настроек подключений, SignalR-клиент |

### Поток запроса

```
Терминал (AiChatTerminal.razor)
  → POST api/AiChat/sessions/{id}/send            (AiChatController, роль Admin)
  → AiChatRunCoordinator.Enqueue                  (один активный запуск на чат, CancellationToken на чат)
  → фоновый scope → AiChatAgentService.RunChatAsync
      → IOptionService.GetOption<AiChatOption>    (выбор подключения)
      → AiChatClientFactory.CreateClient          (IChatClient по провайдеру)
      → chatClient.AsHarnessAgent(...)            (Microsoft Agent Framework, HarnessAgent)
      → agent.RunStreamingAsync(msg, session)     (harness-цикл: модель ↔ инструменты)
          → события в SignalR-группу чата:  AiChatChunk / AiChatToolCall / AiChatToolResult /
                                            AiChatQuestion / AiChatDone / AiChatStopped / AiChatError
  → AiChatHubClient (фронт) → рендер терминала
```

### Хранение чатов

- `AiChatSessionStore` — **HybridCache** (L2 — Postgres distributed cache, уже зарегистрирован в WebApp).
  - Ключи: `aichat:session:{userId}:{chatId}`, индекс `aichat:index:{userId}`, теги `["aichat"]`, TTL 7 дней.
- История диалога хранится дважды:
  - `AiChatSessionState.Messages` — сообщения для UI (пользователь/ассистент/инструменты/ошибки);
  - `AiChatSessionState.SerializedAgentSession` — сериализованная `AgentSession` (`agent.SerializeSessionAsync`) —
    память агента между ходами. При остановке пользователем она НЕ обновляется (возможен незакрытый вызов инструмента).

### Подключения к ИИ-сервисам

Опция `AiChatOption` (`Mars.AiChat.Shared/Options`), форма в админке: Настройки → «ИИ-чат (агент)».

- `AiProviderType`: `OpenAI`, `Qwen` (DashScope compatible-mode), `DeepSeek`, `Ollama`, `Custom`.
- Пустой endpoint → значение по умолчанию (`AiProviderTypeExtensions.GetDefaultEndpoint`).
- `AiChatClientFactory`: Ollama → `OllamaChatClient`; остальные → OpenAI SDK с кастомным `Endpoint`
  (`OpenAIClient` → `GetChatClient(model).AsIChatClient()`), клиенты кэшируются по параметрам подключения.

### Особенности SignalR

- Хаб `/_ws/aichat` (`AiChatHub`) — **без `[Authorize]`**, по аналогии с `ChatHub`:
  «smart»-схема аутентификации Mars не читает `access_token` из query WebSocket-рукопожатия.
  Защита данных — на REST-уровне (роль Admin) и изоляцией по `userId`; chatId — неугадываемый Guid.
- Сервер настроен на `PropertyNamingPolicy = null` (`AddMarsSignalRConfiguration`) —
  клиент обязан делать так же (в `AiChatHubClient.AddJsonProtocol` уже задано).

### Мост «агент → открытая страница» (page bridge)

Агент работает на сервере, а поля формы живут в клиентском Blazor. Инструменты открытой страницы
(`GetOpenPageInfo` / `GetOpenPageFields` / `SetOpenPageField` / `SaveOpenPage` в `Mars.AiChat.Host/Tools/MarsOpenPageTools.cs`)
выполняются через SignalR round-trip:

```
Серверный инструмент (MarsOpenPageTools, экземпляр создаётся на запуск с chatId)
  → AiChatPageBridge.CallPageAsync          (pending-запрос по requestId, таймаут 20 c)
  → событие AiChatPageToolRequest в группу чата
  → AiChatTerminal.HubOnPageToolRequest     (фронт, фильтр по chatId)
  → AiChatPageHandlerHolder.Current         (IAiChatPageHandler открытой страницы)
  → AiChatHubClient.SendPageToolResultAsync → метод хаба PageToolResult
  → AiChatPageBridge.Complete               (TaskCompletionSource → результат возвращается агенту)
```

Страница подключается к мосту так (пример — `EditPostView`):

1. Реализует `IAiChatPageHandler` (`Mars.AiChat.Front/Services/IAiChatPageHandler.cs`):
   `GetInfo()`, `GetFields()`, `SetField(field, value)`, `Save()`.
2. В `OnAfterRender(firstRender)` кладёт себя в `AiChatPageHandlerHolder.Current`,
   в `Dispose()` — снимает (паттерн тот же, что у `AiChatAppService.Setup`).

Нюансы `EditPostView`:

- Контент читается из активного редактора (`blockEditor1.ContentJson` / `codeEditor1.GetValue()` /
  `editor1.GetHTML()`), для ИИ дополнительно отдаётся `contentText` (plain-text извлечение).
- Запись контента — через экземпляр редактора, не через модель: BlockEditor получает Editor.js JSON
  (текст бьётся на абзацы, `BuildBlockEditorJson`), Code — `SetValue`, PlainText — в модель.
  WYSIWYG пока не поддерживается на запись (нет публичного сеттера).
- `SetOpenPageField` меняет форму БЕЗ сохранения (пользователь проверяет и жмёт «Сохранить»);
  `SaveOpenPage` — только по явной просьбе (правило задано в промпте).
- Контекст «какая страница открыта» передаётся в send-запросе (`PageContext` = относительный URL)
  и попадает в инструкции агента.

Чтобы подключить новую страницу к мосту — реализуй `IAiChatPageHandler` и зарегистрируй его так же.

## Как добавить новый скилл (инструмент агента)

Скилл = C#-метод, который агент вызывает через function calling. Пример: инструменты настроек сайта
в `Mars.AiChat.Host/Tools/MarsSiteTools.cs`.

1. **Создай класс инструмента** в `Mars.AiChat.Host/Tools/` (scoped, если нужны сервисы Mars):

```csharp
using System.ComponentModel;

public class MarsContentTools
{
    private readonly IPostService _postService; // любой scoped-сервис Mars

    public MarsContentTools(IPostService postService) => _postService = postService;

    [Description("Получить список постов.")] // описание ОБЯЗАТЕЛЬНО — по нему модель выбирает инструмент
    public string ListPosts([Description("Максимум записей")] int take = 10)
    {
        ...
        return JsonSerializer.Serialize(result);
    }
}
```

Требования:
- `[Description]` на методе и на каждом параметре (иначе модель работает вслепую);
- возвращай строку (лучше компактный JSON) — это то, что увидит модель;
- опциональные параметры — через default-значения;
- методы выполняются **с привилегиями приложения** (HTTP-контекста нет): проверяй данные сам,
  пользователя в фоне нет — передавай `userId` из контроллера, если нужен (`IRequestContext` работает только в HTTP-запросе).

2. **Зарегистрируй в DI** — `MainAiChat.AddMarsAiChat`:

```csharp
services.AddScoped<MarsContentTools>();
```

3. **Подключи к агенту** — `AiChatAgentService`: внедри в конструктор и добавь в массив tools:

```csharp
AIFunctionFactory.Create(_contentTools.ListPosts),
```

4. **Обнови системный промпт** — `AiChatPrompts.BaseInstructions`: коротко опиши, когда применять инструмент.

5. **Собери и проверь** терминал: вызов инструмента виден строками `⚙ имя {аргументы}` и `← результат`.

### Инструмент «спросить пользователя»

`AskUserTool` — образец паттерна «остановка и ожидание ввода»:
инструмент возвращает модели команду остановиться; фронт получает событие `AiChatQuestion`,
показывает вопрос и ждёт следующего сообщения пользователя. Так же можно реализовать
любые human-in-the-loop подтверждения.

### Инструменты настроек сайта

Помимо `MarsSiteTools` (быстрые инструменты для базовых настроек сайта), есть универсальный
`MarsOptionsTools` — управление любой зарегистрированной опцией по имени класса (`IOptionService`):

- `list_site_options` — имена классов опций + флаги `readable`/`writable`;
- `get_site_option(className)` — JSON настройки;
- `update_site_option(className, json)` — полное замещение значения.

Важные детали:

- Десериализация в `SetOptionByClass` чувствительна к регистру — в промпте задано правило
  передавать полный JSON с точным регистром имён полей (сначала прочитать, потом править).
- Списки защиты в `MarsOptionsTools`: `ReadDenied` (секреты: `SmtpSettingsModel`, `AiChatOption`,
  favicon-опции) и `WriteDenied` (плюс `PluginManagerSettingsOption`).
  Появилась новая опция с секретами — добавь её имя в списки.
- Список доступных классов даёт `IOptionService.GetRegisteredOptionClasses()`;
  опции, зарегистрированные другими модулями/плагинами, доступны агенту автоматически.

### Информация о системе

`MarsSystemTools.GetSystemInfo` — инструмент `get_system_info`: версия Mars и git-коммит, ОС/архитектура,
окружение, `IsRunningInDocker` и `IsPM2`, часовые поясы, аптайм и память. Реализация ничего не детектит сама —
использует `IMarsSystemService` (`AboutSystem()`), поэтому источник данных тот же, что у страницы «Настройки → О системе».

### Посты (создание без страницы)

`Mars.AiChat.Host/Tools/MarsPostTools.cs` — инструменты `CreatePost` / `GetPost` / `ListPosts`, работают через
`IPostService` напрямую (страница не нужна). Экземпляр создаётся на каждый запуск с `userId` владельца чата —
он становится автором поста.

- `CreatePost(type, title, contentText, tagsCsv, excerpt)`:
  - тип контента берётся из `IPostService.GetEditModelBlank(type)` (`PostType.PostContentSettings.PostContentType`);
  - текст адаптируется под редактор: BlockEditor → Editor.js JSON (абзацы, `BuildBlockEditorJson`),
    WYSIWYG → `<p>…</p>`, PlainText/Code → как есть;
  - slug генерируется `TextTool.TranslateToPostSlug(title)`, статус — черновик;
  - в ответе агенту возвращается ссылка на страницу редактирования `/EditPost/{type}/{id}`.
- `GetPost(id)` и `ListPosts(type, take)` — чтение; контент отдаётся и «как хранится», и plain-text (`ExtractPlainText`).

Обновление существующего поста сознательно не делается серверным инструментом (полный `UpdatePostQuery`
затирал бы метаполя): редактирование идёт через мост открытой страницы (`SetOpenPageField`),
а «создать» и «прочитать» — серверными инструментами.

## Как добавить новое событие сервер → клиент

1. Константа в `Mars.AiChat.Shared/SignalR/AiChatHubEvents.cs` (с сигнатурой в комментарии).
2. Отправка в `AiChatAgentService`: `SendCoreAsync(group, AiChatHubEvents.Xxx, [chatId, runId, ...])`.
3. Подписка в `Mars.AiChat.Front/Services/AiChatHubClient.cs`: `connection.On<...>` + событие.
4. Обработка в `AiChatTerminal.razor.cs` (подписка в `SubscribeHub`, отписка в `Dispose`).

Помни: аргументы событий — простые типы (Guid/string); сложные DTO сериализуются с `PropertyNamingPolicy = null`.

## Как развивать агента (roadmap-идеи)

Реализовано: настройки сайта и любые опции, информация о системе, создание/чтение постов,
мост открытой страницы редактирования поста (чтение/правка полей, сохранение по запросу).

- **Редактирование поста без страницы**: сейчас серверный `UpdatePost` сознательно опущен
  (полный `UpdatePostQuery` затёр бы метаполя); нужен аккуратный partial-update поверх `GetDetail`.
- **WYSIWYG-контент**: запись пока не поддерживается (нет публичного сеттера у `WysiwygEditor`);
  добавить `SetHTML` и подключить в `SetContentValue`.
- **Мост для других страниц**: реализовать `IAiChatPageHandler` для новых страниц (пользователи, настройки).
- **Подтверждения опасных действий**: расширить `AskUserTool` до `confirm_action(action)` с ответом да/нет.
- **Другие скиллы**: пользователи (`IUserService`), медиа (`IMediaService`), ноды-флоу, плагины, Docker.
- **Выбор подключения на чат**: сейчас берётся `DefaultConnectionName`; можно хранить подключение в `AiChatSessionState`.

## Известные ограничения

- Один активный запуск на чат (повторный `send` → 466 UserAction «чат уже обрабатывается»).
- При остановке («стоп») текст, уже показанный стримом, досохраняется в историю при следующей удачной сессии
  только если она была flushed до вызова инструмента — см. `FlushText`/`OnStoppedAsync`.
- `MaximumIterationsPerRequest = 15` — защита от бесконечного цикла инструментов.
- UI-сообщения Tool в истории: вызов (`IsToolResult=false`, Content = JSON аргументов) и результат (`IsToolResult=true`).
