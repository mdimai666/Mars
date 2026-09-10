# План переделки ActionCenter — Command Palette в стиле VS Code

> **Статус: P0–P5 реализованы (2026-08-18); поиск настроек — 2026-08-20.**
> Задача-источник: [CreateNewActionCenterPrompt.md](./Prompts/CreateNewActionCenterPrompt.md).
> Предыстория: XActions как командная шина добиты по [XActionsPlan.md](./XActionsPlan.md) (P1–P4);
> палитра — главный потребитель этого реестра.

## Принятые решения (2026-08-18)

1. **Форм-фактор — модалка по центру сверху** (VS Code/GitHub-стиль): оверлей + затемнение,
   фокус всегда в инпуте, закрытие по Esc/клику вне. Не FluentUI — свой HTML/CSS.
2. **Режимы по префиксу**: пусто — совмещённый список (команды сверху, разделитель, результаты
   поиска; при пустом запросе — недавние страницы); `>` — только команды (фильтр локальный);
   `#` — только поиск (серверный запрос).
3. **Фронтовые действия** — новый `XActionType.FrontAction`: метаданные регистрируются как
   обычные XActions (система видит их в `/api/Act/list`), исполнение — на клиенте через
   реестр раннеров в `AppFront.Shared`. Сами действия «тёмная тема»/«язык» не реализуем —
   только инфраструктура.
4. **Поиск — провайдерная модель**: `ICentralSearchProvider` в DI, `CentralSearchService` —
   тонкий агрегатор. Любой модуль/плагин регистрирует провайдер обычной DI-регистрацией.
   Из источников убираем NavMenu и XActions (команды фильтруются на клиенте), оставляем
   Posts + PostTypes отдельными провайдерами.
5. **Эндпоинт поиска выносится** из `ViewModelController` в отдельный `SearchController`.
6. **Недавние страницы и MRU команд** — localStorage (Blazored.LocalStorage уже подключён).
7. `GeneralSectionActions.razor` **остаётся** — проверено: он зарегистрирован в
   `AppAdmin/Program.cs` как глобальная секция и рендерится `ContentWrapper`/`StandartEditContainer`
   на стандартных страницах (кнопка «Действия»). «Дубля» `GActionCenterContainer` в дереве нет —
   единственный экземпляр заменяется новой палитрой и удаляется.

---

## 1. Как сейчас (as-is)

- `src/AppAdmin/Shared/GActionCenterContainer.razor(.cs)` — дропдаун под `FluentSearch` в шапке
  (`HeaderAdmin1.razor`), виден при вводе ≥1 символа. Клавиатурной навигации по списку нет.
  Команды берутся живьём через `GET /api/Act/list` (это оставляем), группируются по `Category`;
  список «локальных страниц» (`definedLocals`: Настройки, Записи, Users, Logout, Node-Red,
  Source code) захардкожен в компоненте.
- `src/Mars.WebApp/Controllers/ViewModelController.cs` — `GlobalSearch` рядом с
  `InitialSiteDataViewModel`, валидация maxCount/длины запроса.
- `src/Mars.Host/Services/CentralSearchService.cs` — агрегатор на 4 жёстко вшитых источника:
  XActions (`IActionManager`), PostTypes, Posts, NavMenu (dev-меню). Релевантность — константы
  (10/5/2/1). Провайдерной модели нет.
- Клиент: `ViewModelService.GlobalSearch` (`AppFront.Shared`) → `vm/ViewModel/GlobalSearch`.
- Хоткей: F1 → `AdminLayout` → `HeaderAdmin1.FocusActionCenter()` (просто фокус поля).
- XActions: двухслойная модель (Act/XAction), эффекты, формы — палитра строится поверх.

### Проблемы

1. Нет клавиатурного выбора, нет модалки — палитра «прилеплена» к шапке.
2. Поиск команд на сервере дублирует клиентский список (палитра уже имеет живой реестр).
3. Источники поиска вшиты в сервис — регистрация из модулей/плагинов невозможна.
4. Хардкоженные «локальные страницы» вместо недавних + меню.
5. Фронтовых (клиентских) действий в модели XActions нет.

---

## 2. Референсы (что берём)

**VS Code**: виджет quick-input отделён от провайдеров контента; команды фильтруются на клиенте
fuzzy-скорингом (подпоследовательность + бонус за начало слова/camelCase); недавно использованные
команды сверху при пустом запросе (MRU в локальном сторадже); плоский список с группами
`Category` и разделителями; полная клавиатура (↑/↓/Enter/Esc); модалка по центру сверху.

**GitHub command palette (Ctrl+K)**: кнопка-триггер в шапке вида «Поиск или команда…»,
открывающая модалку; совмещённый список команд и результатов при пустом запросе.

---

## 3. Целевая модель

### 3.1. Серверный поиск — провайдеры

```csharp
// Mars.Host.Shared/Services/ICentralSearchProvider.cs
public interface ICentralSearchProvider
{
    /// <summary>Порядок выдачи в агрегированном результате.</summary>
    int Order { get; }
    Task<IReadOnlyCollection<SearchFoundElement>> SearchAsync(
        string query, int maxCount, CancellationToken cancellationToken);
}
```

- `CentralSearchService` — агрегатор над `IEnumerable<ICentralSearchProvider>` из DI:
  параллельный вызов провайдеров, `OrderBy(Order)` → `OrderByDescending(Relevant)` внутри группы,
  общий `Take(maxCount)`.
- Провайдеры в `Mars.Host`: `PostTypesSearchProvider`, `PostsSearchProvider`
  (логика выносится из `Aggregator` один в один). NavMenu и XActions — удаляются.
- Регистрация откуда угодно: `services.AddScoped<ICentralSearchProvider, MyProvider>()`
  в модуле/плагине — агрегатор подхватит.

### 3.2. Эндпоинт

- Новый `SearchController` (`src/Mars.WebApp/Controllers`), `[Route("api/[controller]")]`,
  `[Authorize]`, действие `Query(string text, int maxCount = 10, CancellationToken)` —
  с той же валидацией (maxCount ≤ 30, текст ≥ 2 символов).
- `GlobalSearch` из `ViewModelController` удаляется.
- Клиент: `SearchServiceClient` в `Mars.WebApiClient` (по образцу `ActServiceClient`),
  свойство на `IMarsWebApiClient`; `ViewModelService.GlobalSearch` удаляется,
  палитра ходит через `_client.Search.Query(...)`.

### 3.3. Фронтовые действия

- `XActionType` += `FrontAction` (`src/Mars.Shared/Contracts/XActions/XActionCommand.cs`).
- `XActionBuilder.FrontAction()` — привязка исполнения «клиентский раннер»:
  без Handler/Link, валидация билдера это допускает.
- `XActionManager.Inject`: `FrontAction` на хосте → ошибка исполнения
  («команда выполняется на клиенте») — сервер их не исполняет.
- Клиент (`AppFront.Shared`):
  ```csharp
  public interface IFrontActionRunner
  {
      string ActionId { get; }
      Task<XActResult> ExecuteAsync(IReadOnlyDictionary<string, string> args, CancellationToken ct);
  }
  ```
  Реестр в DI (`IEnumerable<IFrontActionRunner>` → словарь по ActionId).
- `ActAppService.Inject`: `Type == FrontAction` → раннер из реестра (нет раннера — тост-ошибка);
  результат интерпретируется как обычно (тост/эффекты). Формы аргументов работают тем же путём.
- Демо для проверки (DEBUG): `Mars.Debug.FrontDemo` — тост «выполнено на клиенте».

### 3.4. Палитра (UI)

Каталог `src/AppAdmin/Shared/ActionCenter/`:

- `ActionCenterService` (scoped) — состояние открыто/закрыто, `Open()`/`Close()` + событие;
  вызывается хоткеями и триггером в шапке.
- `ActionCenter.razor(.cs)` — модалка: инпут + список. Рендерится в `AdminLayout`.
- `action-center.less` — стили в духе VS Code (подключается в `style.less`).

Поведение:

- **Открытие**: F1 (заменяет текущий фокус поля), новый Ctrl+K, кнопка-триггер в `HeaderAdmin1`
  («Поиск или команда…» вместо `FluentSearch`).
- **Контент при пустом запросе** (совмещённый режим): MRU команд → команды по `Category`
  (живой `GET /api/Act/list`, фильтр `System`) → разделитель → недавние страницы.
- **Ввод без префикса**: тот же список, команды фильтруются локально (fuzzy: подпоследовательность,
  бонус за начало слова), секция поиска — результат `Search.Query` (debounce ~150 мс).
- **`>`**: только команды, префикс в список не попадает.
- **`#`**: только серверный поиск.
- **Клавиатура**: ↑/↓, Enter — исполнить, Esc — закрыть, выделенный пункт скроллится в видимую
  область, ховер мышью синхронизирован с выделением.
- **Исполнение**: команда → `ActAppService.Inject` (учитывает формы аргументов), палитра
  закрывается, id пишется в MRU; страница/результат → `NavigateTo`, url пишется в недавние.
- `definedLocals` и служебный кейс `source-code` → в XActions-линки, где их ещё нет
  (часть уже есть в `ConfigureActions`), либо в недавние — хардкод из палитры уходит.

### 3.5. Недавние страницы и MRU

- `RecentPagesService` (scoped, `AppAdmin/Shared/ActionCenter/`): подписка на
  `LocationChanged`, запись в localStorage через `ILocalStorageService`:
  `{ url, title, ts }`, дедупликация по url, лимит ~10, title — из dev-меню при совпадении.
- MRU команд — отдельный ключ localStorage, лимит ~8.

---

## 4. Фазы внедрения

### P0 — переезд эндпоинта поиска ✅
`SearchController` + `SearchServiceClient` + `IMarsWebApiClient.Search`; удаление
`GlobalSearch` из `ViewModelController` и `ViewModelService`.
Отклонение: параметр `text` стал `string?` — non-nullable строку ASP.NET неявно требует
(`[ApiController]`), и whitespace-запрос падал в 400 «text field is required» вместо
замысла `IsNullOrWhiteSpace → []`. Nullable делает ветку достижимой. Клиенту сохранён
дефолт `maxCount = 20` (как у старого `ViewModelService.GlobalSearch`).

### P1 — провайдерная модель поиска ✅
`ICentralSearchProvider` (`Mars.Host.Shared`), агрегатор-`CentralSearchService`
(параллельный опрос, Order провайдера → Relevant внутри, общий Take), провайдеры
`PostTypesSearchProvider`(Order=10)/`PostsSearchProvider`(Order=20) в `Mars.Host`,
NavMenu/XActions из поиска убраны. Тесты агрегатора: `Test.Mars.Host/Services/CentralSearchServiceTests.cs`.

### P2 — фронтовые действия ✅
`XActionType.FrontAction`, `XActionBuilder.FrontAction()` (нельзя совмещать с Handler/Link),
`XActionManager.Inject` на хосте возвращает `ToastError`. Клиент: `IFrontActionRunner`
(`AppFront.Shared`), реестр в `ActAppService` (Dictionary по ActionId из `IEnumerable<>` DI),
общее применение эффектов вынесено в `PresentResult`. DEBUG-демо `Mars.Debug.FrontDemo`:
метаданные в `ConfigureActions` + раннер `AppAdmin.Shared.FrontDemoActionRunner`.
Тесты: `Test.Mars.Host/XActions/XActionBuilderFrontActionTests.cs` (4),
`Mars.WebApiClient.Integration.Tests/Tests/Acts/FrontActionTests.cs` (2).

### P3 — палитра ✅
`src/AppAdmin/Shared/ActionCenter/`: `ActionCenterService` (scoped, open/close + событие),
`ActionCenter.razor(.cs)` — модалка без FluentUI: инпут + список секциями, режимы
(пусто / `>` / `#`), локальный fuzzy-фильтр команд (префикс 100 / подстрока 80 / подпоследовательность 40),
серверный поиск с debounce 150 мс, клавиатура (↑/↓/Enter/Esc, scroll-into-view), MRU команд
в localStorage (лимит 8). Стили `action-center.less` (VS Code-стиль) + кнопка-триггер в шапке.
`AdminLayout`: `<ActionCenter />`, хоткеи F1/Ctrl+K → `Toggle()`. `HeaderAdmin1`: триггер
вместо `FluentSearch`. `GActionCenterContainer` и `global_search_container.less` удалены.
JS: `d_actionCenter_scrollIntoView` в `scripts.js`.

### P4 — недавние страницы ✅
`RecentPagesService` (scoped): подписка на `LocationChanged`, localStorage (лимит 10,
дедупликация по url), заголовок из dev-меню при совпадении url. Секция «Недавние страницы»
в палитре при пустом запросе. Инициализация ленивая (`EnsureInitializedAsync`) при открытии палитры.

---

## 4b. P5 — улучшения палитры (согласовано 2026-08-18)

Итоги обсуждения с пользователем (решения зафиксированы):

- **Закреплённые команды** в пустом виде, как в VS Code (встроенные в палитру, не XActions):
  1. **Перейти на страницу** — под-режим выбора страницы из админки через
     `IBlazorPagesService.GetStaticRoutedPages([сборка AppAdmin])` (уже используется в
     `DevAdminConnectionService`), фильтр ролей, локальный fuzzy по DisplayName/маршруту,
     Esc — назад в главный вид.
  2. **Выполнить команду** — вводит `>`; при пустом вводе в `>` показываются **все
     команды** (со скроллом), **рекомендуемые — сверху**.
  3. **Поиск** — вводит `#`.
  4. **Открыть чат с ИИ** — `IAiChatAppService.Open()` (`AiChatContainer` в `App.razor`).
  5. **Редактор нод** — навигация `/dev/nodered` (`NodeRedPage @page "/nodered"`).
- **Подзаголовки категорий команд убрать** (засоряют). Пункты **в одну строку**: название,
  затем с отступом хвост. Для команд хвост = **Description** (если нет — Category);
  для результатов поиска = Description; для страниц = Url. Category у команд остаётся
  видимой только как fallback-хвост.
- **Рекомендуемые команды**: в метаданные `XActionCommand` и в `XActionBuilder` добавить
  `Recommended(int priority = 1)` (число: и флаг «рекомендуемая», и порядок).
  В `>` без ввода — **все команды** (со скроллом), команды с `Recommended > 0` сверху
  (по убыванию priority), остальные по алфавиту.
- **Недавние** после разделителя — **два блока подряд без заголовков**: недавние команды,
  затем недавние страницы.
- **Индикатор поиска**: только **маленький спиннер справа в строке ввода**, пока идёт
  серверный запрос (без строки «Идёт поиск…»).
- **Поведение ввода без префикса** (по умолчанию) оставляем: fuzzy по командам + разделитель
  + серверный поиск. Закреплённые команды видны только при пустом вводе.

### Реализация P5 (подшаги) — ✅ все выполнены 2026-08-18

- **P5.1 Recommended** ✅: `XActionCommand.Recommended` (int?), `XActionBuilder.Recommended(int=1)`;
  в `ConfigureActions` помечены ClearCache(10), CreatePostTypePresentationTemplate(5).
- **P5.2 Одна строка** ✅: `.ac-item` — одна строка (иконка + название + хвост с отступом, ellipsis);
  заголовки категорий/секций убраны (свойство `Header` удалено из `PaletteSection`), разделение —
  только `.ac-divider`. Хвост команд = Description ?? Category.
- **P5.3 Закреплённые команды** ✅: 5 встроенных пунктов (`PaletteItemKind.Pinned` + `PinnedCommand`),
  под-режим выбора страницы (`PaletteView.Pages`, `IBlazorPagesService.GetStaticRoutedPages([App])`,
  фильтр ролей, Esc — назад); «Выполнить команду»→`>`, «Поиск»→`#`, ИИ-чат `IAiChatAppService.Open()`,
  редактор нод `/dev/nodered`.
- **P5.4 Пустой вид** ✅: закреплённые → разделитель → недавние команды → недавние страницы
  (два блока подряд, без заголовков). MRU остался `List<string>` (без ts — блоки не смешиваются).
- **P5.5 Спиннер** ✅: флаг `_searching`, маленький `.ac-spinner` справа в инпуте на время запроса.
- Cache-busting: `MarsAppVersion` поднят до `0.7.8-alpha.37` (правлен `style.css`).

Тесты P5: `Test.Mars.Host/XActions/XActionBuilderRecommendedTests.cs` (3),
`ListActTests.List_ShouldCarryRecommendedPriority` (1). Регрессия ListAct/FrontAction/
QuerySearch/InjectAct/CentralSearch — вся зелёная.

Пост-фикс P5 (по фидбеку, 2026-08-18):
- Глюк каретки: ArrowUp/ArrowDown двигали каретку в инпуте при навигации по списку.
  Фикс — JS-хук `d_actionCenter_preventArrowCaret` (preventDefault только для этих клавиш),
  вызывается из `OnAfterRenderAsync` при фокусе.
- Режим `>` без ввода: показываются ВСЕ команды (со скроллом), рекомендуемые — сверху
  (было: только топ-10 рекомендуемых). `RecommendedTake` удалён.
- `MarsAppVersion` → `0.7.8-alpha.38` (правлен `scripts.js`).
- Фикс «быстрого набора» поиска: устаревшие запросы могли перезатирать результат свежих
  (запрос для опечатки возвращал пусто и перекрывал исправленный запрос). Фикс — `CancellationToken`
  пробрасывается в `ISearchServiceClient.Query` (`GetJsonAsync(..., HttpCompletionOption, ct)`),
  вытесненный запрос реально отменяется; в ветке «запрос очищен» теперь гасится зависший
  `_searchCts`; применение результата только по `seq == _searchSeq`; `InvokeAsync(StateHasChanged)`.

---

## 4c. Поиск настроек (опций) в палитре — ✅ реализовано 2026-08-20

Поиск по зарегистрированным настройкам (например, `ApiOption`) — **локально в палитре**
(вариант «A»), без серверного провайдера: источник — клиентский `IOptionsFormsLocator`
(`RegisteredFormsAutoShow()` — ровно то множество, что показывает меню страницы Настроек,
поэтому тупиковых пунктов «Option type Form not found» не бывает).

- `ActionCenter.razor.cs`: `[Inject] IOptionsFormsLocator`, кеш `OptionEntry`
  (Title = Display формы, Description = Display класса опции либо «Настройка»,
  SearchText = «Display формы + Display класса + имя класса»,
  Url = `/dev/Settings/Option/{FullName с "+" вместо "."}` — тот же, что строит `ASideOptions`),
  `FilterOptions` через существующий `MatchScore`, `PaletteItemKind.Option` + иконка `bi bi-gear`.
- Секции: ввод без префикса — команды → настройки → серверный поиск (разделители между
  непустыми); режим `#` — настройки → серверный поиск, но при пустом запросе после `#`
  настройки не показываются (по фидбеку пользователя). `>` не изменён.
- Исполнение — `NavigateTo(url)` в общей ветке Page/SearchResult/RecentPage/Option.
- Сервер, контракты поиска и стили не менялись; `MarsAppVersion` не поднимался (чистый WASM C#).

---

## 5. Верификация

Точечно — без прогонов всего сьюта:

- `dotnet build Mars.slnx`.
- Интеграционные тесты нового эндпоинта через `SearchServiceClient`: валидация
  (короткий запрос → пусто, maxCount > 30 → ошибка), результат от двух провайдеров,
  фильтр System-команд отсутствует (команд больше нет в выдаче).
- Юнит/интеграционные тесты агрегатора: порядок провайдеров (`Order`), объединение результатов.
- XActions: `Inject` для `FrontAction` на хосте → ошибка; валидация билдера
  (FrontAction без Handler/Link проходит; Handler+FrontAction — нет); клиентский реестр
  раннеров резолвит по id (тест `ActAppService`-логики, если возможно без браузера).
- UI палитры — визуально при разработке (как в P3/P4 XActionsPlan).

## Вне скоупа

- Действия «тёмная тема» и «переключить язык» — только инфраструктура под них.
- Хоткеи как настраиваемая система keybindings (Ctrl+K/F1 — фиксированные).
- Поиск по NavMenu и по XActions на сервере.
- Изменения `XActionsDropDown`/`GeneralSectionActions` (контекстные действия страниц).
- Палитра в публичном фронте (`AppFront`) — только админка.
