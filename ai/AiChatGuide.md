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

## Как добавить новое событие сервер → клиент

1. Константа в `Mars.AiChat.Shared/SignalR/AiChatHubEvents.cs` (с сигнатурой в комментарии).
2. Отправка в `AiChatAgentService`: `SendCoreAsync(group, AiChatHubEvents.Xxx, [chatId, runId, ...])`.
3. Подписка в `Mars.AiChat.Front/Services/AiChatHubClient.cs`: `connection.On<...>` + событие.
4. Обработка в `AiChatTerminal.razor.cs` (подписка в `SubscribeHub`, отписка в `Dispose`).

Помни: аргументы событий — простые типы (Guid/string); сложные DTO сериализуются с `PropertyNamingPolicy = null`.

## Как развивать агента (roadmap-идеи)

- **Сценарий «создай пост»**: инструменты на `IPostService`/`IPostTypeService` + промпт-инженеринг;
  для «пишет прямо в форме» нужен мост UI-автоматизации (JS-события/директивы в страницы админки).
- **Контекст текущей страницы**: фронт знает URL (`NavigationManager`) — передавать в `send`-запросе
  и добавлять в инструкции агента («ты находишься на странице …»).
- **Подтверждения опасных действий**: расширить `AskUserTool` до `confirm_action(action)` с ответом да/нет.
- **Другие скиллы**: пользователи (`IUserService`), медиа (`IMediaService`), ноды-флоу, плагины, Docker.
- **Выбор подключения на чат**: сейчас берётся `DefaultConnectionName`; можно хранить подключение в `AiChatSessionState`.

## Известные ограничения

- Один активный запуск на чат (повторный `send` → 466 UserAction «чат уже обрабатывается»).
- При остановке («стоп») текст, уже показанный стримом, досохраняется в историю при следующей удачной сессии
  только если она была flushed до вызова инструмента — см. `FlushText`/`OnStoppedAsync`.
- `MaximumIterationsPerRequest = 15` — защита от бесконечного цикла инструментов.
- UI-сообщения Tool в истории: вызов (`IsToolResult=false`, Content = JSON аргументов) и результат (`IsToolResult=true`).
