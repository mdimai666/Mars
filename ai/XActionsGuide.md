# XActionsGuide — командная шина и Command Palette

Гайд для агента: XActions (универсальная шина действий платформы) + ActionCenter
(палитра команд) + центральный поиск. История инициатив (планы схлопнуты):
`git show 3b13b36a:ai/XActionsPlan.md`, `git show 3b13b36a:ai/ActionCenterPlan.md`.
Смежные: `ai/CloudyGuide.md` (UI админки), `ai/FeatureIntegrationGuide.md`,
`ai/PluginCreationGuide.md`.

## Где что лежит

- Модуль `src/Mars.Modules/Mars.XActions.*`:
  - `Contracts` — `XActionCommand.cs` (модель команды/аргументов/результата, `IAct`,
    `XActionType`), `XActionBuilder.cs` (fluent-регистрация), `XActionEffect.cs` (union эффектов).
  - `Abstractions` — `IActionManager`, `XActionsServiceCollectionExtensions.AddXActionHandlers`.
  - `Host` — `XActionManager` (singleton-реестр), `ActController` (`api/Act/*`),
    `MainXActions.AddMarsXActionsHost`.
- Точки регистрации команд: `src/Server/Mars.Server/MainServer.cs` (хост-акты),
  `Mars.Cms.Host/XActions/CmsXActions.cs`, `Mars.Media.Host/MediaXActions.cs`;
  админ-контексты и админ-команды — `src/Admin/Mars.Admin.Host/XActions/AdminXActionsOverlay.cs`.
- Клиент: `ActAppService` + `IFrontActionRunner` (`src/Admin/Mars.Admin.Framework/Services/`).
- UI: `XActionsDropDown` (Framework, контекстное меню страниц), `XActionFormDialog` +
  `FluentDialogXActionFormPresenter` (`src/Mars.Admin/Shared/`), палитра
  `ActionCenter` (`src/Mars.Admin/Shared/ActionCenter/`, стили `action-center.less`).
- Центральный поиск: `ICentralSearchProvider` (`Mars.Cms.Abstractions/Services/`),
  `CentralSearchService` + `SearchController` (`Mars.Cms.Host`), клиент `_client.Search`.
- Nodes: `ExecXActionNode` (TypeId `core.ExecXActionNode`) + `ExecXActionNodeImpl`
  (`Mars.Nodes.Core[.Implements]/Nodes/Connections/`); динамические команды-потоки —
  `IXActionCommandsProvider` через `AddActionsProvider`.

## Как работает

- **Двухслойная модель VS Code**: Act = чистый хэндлер `IAct` (без атрибутов и метаданных;
  сканируется из явно зарегистрированных сборок `AddXActionHandlers(assembly)`, живёт в DI);
  XAction = команда (метаданные + привязка исполнения), регистрируется ИМПЕРАТИВНО
  `actionManager.Add(a => a.Id(...).Label(...).Handler<TAct>())` — аналог
  `contributes.commands` + `registerCommand` в одном месте. XLink — команда без хэндлера
  (навигация). Один Act может стоять за несколькими командами.
- **Id** — строковые константы вида `mars.category.name`; regex допускает одиночный сегмент
  и `+`/`-` в сегментах. Существующие id НЕ переименовывать (сохранены в потоках
  `ExecXActionNode.CommandId` и разметке).
- **Аргументы именованные** (`IReadOnlyDictionary<string,string>`), валидация до исполнения
  (required/неизвестный id). Схема `XActionArgument`: String/Number/Bool/Choice;
  варианты выбора — `XActionOption { Key, Label }` (в вызов уходит Key): статические
  `Options` в схеме + динамические `OptionsSource` → `GET api/Act/options/{ключ}`
  (`AddOptionsSource(key, factory)` / `IXActionOptionsSource`).
- **Эндпоинты** (`ActController`): `POST api/Act/Inject` (тело `XActionCommandCall`),
  `GET api/Act/list` (без System-команд), `GET api/Act/options/{sourceKey}`.
- **Результат и эффекты**: `XActResult` (Ok/Message/MessageIntent/Effects), фабрики `Toast*`,
  билдеры `WithNavigate/WithEvent/Then`. Эффекты — РЕКОМЕНДАЦИИ: `NavigateEffect`,
  `NextActionEffect`, `TriggerEventEffect`, `CustomEffect` (полиморфизм по `kind`;
  у Custom — `CustomKind`, имя `Kind` занято дискриминатором). Неизвестные kind игнорируются.
  `ActAppService` применяет только тост/navigate/событие в клиентскую шину `Q.Root.Emit`;
  `NextAction`/`Custom` автоматически НЕ исполняются — возвращаются вызывающему.
  `ExecXActionNodeImpl` кладёт результат с эффектами в payload — цепочки строятся потоком.
- **Формы вызова** (порядок): аргументы переданы → сразу Inject; кастомная форма
  (фронтовый реестр actionId → компонент) → она; есть схема аргументов → генерик-модалка
  `XActionFormDialog` (динамические Options подгружаются перед отрисовкой); иначе → Inject.
- **FrontAction**: метаданные регистрируются как обычно (`XActionBuilder.FrontAction()`,
  нельзя совмещать с Handler/Link), исполнение — на клиенте через реестр
  `IFrontActionRunner` в `ActAppService`; хост такую команду не исполняет (ошибка).
  Демо — `Mars.Debug.FrontDemo` + `FrontDemoActionRunner`.
- **Палитра ActionCenter**: модалка по центру сверху (НЕ FluentUI, свой HTML/CSS),
  хоткеи F1/Ctrl+K, триггер в топбаре. Режимы: пусто — закреплённые команды → недавние
  команды → недавние страницы; `>` — только команды (локальный fuzzy: префикс 100 /
  подстрока 80 / подпоследовательность 40; без ввода — ВСЕ команды, `Recommended > 0`
  сверху по убыванию priority); `#` — настройки + серверный поиск (debounce 150 мс).
  Поиск настроек — ЛОКАЛЬНО через `IOptionsFormsLocator.RegisteredFormsAutoShow()`
  (url `/dev/Settings/Option/{FullName с "+" вместо "."}`), серверного провайдера нет.
  MRU команд (лимит 8) и недавние страницы (лимит 10, `RecentPagesService`, подписка
  на LocationChanged) — localStorage. Пункты в одну строку: название + хвост
  (Description ?? Category; у страниц — Url).
- **Центральный поиск**: провайдерная модель — `CentralSearchService` параллельно опрашивает
  `IEnumerable<ICentralSearchProvider>` из DI, сортировка Order провайдера → Relevant внутри,
  общий Take. Провайдеры: PostTypes (Order=10), Posts (Order=20). NavMenu и XActions
  в серверный поиск НЕ входят (команды фильтруются на клиенте). Валидация: maxCount ≤ 30,
  текст ≥ 2 символов; параметр `text` — `string?` (иначе whitespace-запрос падает в 400).

## Как добавить (рецепты)

- **Новая команда**: `IAct` в модуле → `services.AddXActionHandlers(typeof(MyAct).Assembly)`
  → `actionManager.Add(a => a.Id("mars.x.y").Label("…").Category("…").Handler<MyAct>())`
  в Main-классе модуля. Контекстные меню админки — `AddFrontContexts(id, typeof(Page).FullName)`
  в `AdminXActionsOverlay` (доменный владелец не знает про страницы админки).
- **Аргумент-селектор**: `.Argument(...)` с `Options` (статика) или `OptionsSource` +
  `actionManager.AddOptionsSource(key, factory)` (динамика; образец —
  `CreateMockPostsAct.PostTypesOptionsSource` в `MainCms`).
- **Фронтовое действие**: `XActionBuilder.FrontAction()` + реализация `IFrontActionRunner`
  в DI клиента.
- **Провайдер поиска**: `services.AddScoped<ICentralSearchProvider, MyProvider>()`
  в модуле/плагине — агрегатор подхватит.
- **Плагин**: `AddXActionHandlers(pluginAssembly)` + регистрация команд в bootstrap-хуке.

## Тесты

- `tests/Mars.Server.Tests/XActions/` — билдер (FrontAction, Recommended).
- `tests/Mars.Server.Tests/Services/CentralSearchServiceTests.cs` — агрегатор.
- `tests/Mars.Integration.Tests/Controllers/Acts/` — Inject, регенерация meta-values.
- `tests/Mars.WebApiClient.Integration.Tests/Tests/Acts/` — Inject/List/FrontAction через клиент.

## Грабли

- **Переименование админ-страницы ломает контексты**: `FrontContextId` — строки
  `typeof(Page).FullName` в `AdminXActionsOverlay`; при rename страницы контексты молча
  перестают совпадать (команда пропадает из меню страницы). Обновлять overlay вместе с rename.
- Эффекты-события уходят в КЛИЕНТСКУЮ шину `Q.Root.Emit` (не серверный IEventManager).
- Палитра: устаревшие поисковые запросы могут перезатирать свежие — результат применяется
  только по `seq == _searchSeq`, CancellationToken пробрасывается в `ISearchServiceClient.Query`.
- ArrowUp/Down в инпуте палитры двигали каретку — JS-хук `d_actionCenter_preventArrowCaret`
  (preventDefault), вызывать при фокусе.
- `System`-команды скрыты из `api/Act/list` и меню, но вызываются кодом/потоками/API.
- Генерик-форма и палитра живут в админке; в публичном фронте палитры нет.

## Инварианты

- Автоцепочек НЕТ (ни серверных, ни фронтовых): `NextActionEffect` — рекомендация,
  решение принимает вызывающий.
- Существующие id команд не переименовывать; нормализация — только для новых.
- Act не знает, что он команда (никаких атрибутов/метаданных на хэндлере).
- Валидация аргументов — до исполнения, не ручными проверками в актах.

## Отклонено

- Атрибутная саморегистрация (`RegisterXActionCommandAttribute` + статические `XAction`
  на классах) — удалена: декларация разрывалась на два места, метаданные терялись.
- Спецтипы аргументов `PostType`/`Front` с серверным обогащением — заменены универсальными
  `XActionOption` + `OptionsSource` (сервер не знает про типы сущностей).
- Серверный поиск по NavMenu и XActions; хардкод «локальных страниц» в палитре.
- Палитра в публичном фронте; настраиваемые keybindings (F1/Ctrl+K фиксированы).
- WPF-стиль атрибутной генерации команд — императивная регистрация явнее.

## Что ещё не сделано

- Инструменты AiChat для XActions (модель эффектов не блокирует — будущий `AiChatEffect`;
  плагины не могут регистрировать `JsonDerivedType` в рантайме — для них `CustomEffect`).
- Хоткеи как настраиваемая система.
- Место `XActionsDropDown` в Cloudy-админке не определено (см. бэклог `ai/CloudyGuide.md`).