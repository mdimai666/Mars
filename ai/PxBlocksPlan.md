# PxBlocks — план

> PxBlocks — визуальный редактор блоков в духе Microsoft PXT (MakeCode/Blockly) на Blazor.
> Живой план инициативы: Этапы 0–14 завершены, открытые пункты — в «Куда двигаться дальше».
> Полная история этапов и as-is до объединения документа:
> `git show ea65cf08:src/Mars.PxBlocks/Mars.PxBlocks.Workspace/PLAN.md` и
> `git show ea65cf08:src/Mars.PxBlocks/AGENTS.md` (объединены сюда 2026-09-26).
> Смежные: `ai/ProjectStructureGuide.md` (конвенции решения),
> `ai/Prompts/PxBlocksEmbedInNodeEditorPrompt.md` (исходный промпт встраиваемости — Этапы 9–11).

## Миссия

Изначальная формулировка (2026-08): создать аналог Microsoft PXT (форк Blockly) на Blazor.

- Редактор, где можно создавать блоки из Toolbox: стыковать, удалять, перемещать — всё,
  что есть в оригинальном PXT (Blockly).
- Код PXT скачан для референса: `C:\js\2026\microsoft-pxt`.
- Организация кода — по образцу `Mars.Nodes.Workspace` (проект, похожий на Node-RED).
- Без симулятора и без кодогенерации (решение пользователя).

Референсы (вдохновение):

- Наглядный симулятор: [Microsoft MakeCode for micro:bit](https://makecode.microbit.org/#editor)
- Иллюстрация блоков: [Movement — Documentation | Niryo](https://docs.niryo.com/api/blockly/movement/)
- Blockly с 2025-11 живёт в Raspberry Pi Foundation: доки — docs.blockly.com,
  репозиторий — `github.com/RaspberryPiFoundation/blockly`.

## Статус

Этапы 0–14 ✅: редактор (0–6), исполнение в .NET (7), запуск на сервере — `Mars.PxBlocks.Host` (8),
встраиваемость: контексты, песочница/форма (9), чистое встраивание: REST запуска объявляет хост (10),
состояние запуска и имплементации по запуску (11), браузерные скрипты — Playwright-контекст стенда (12),
английские лейблы и добор блоков до parity (13), расширения текста, массивы MakeCode,
редактор функций PXT (14). Тесты: `tests/Mars.PxBlocks.Tests` (133 на 2026-09-26).

## Куда двигаться дальше

### Структурные вопросы (ревью против `ai/ProjectStructureGuide.md`, 2026-09-26)

Семейство соответствует конвенциям решения: суффиксы `.Contracts`/`.Abstractions`/`.Host`,
направленность ссылок (чужих `.Host` нет, Contracts WASM-чистый), хуки `AddPxBlocks()`/`UsePxBlocks()`
в `MainPxBlocks.cs`, тесты `tests/Mars.PxBlocks.Tests`, виртуальные папки slnx. Открыты:

- [ ] A. В `ai/ProjectStructureGuide.md` нет `src/Mars.PxBlocks`: добавить строку в карту `src/`
      + сноску про суффиксы Nodes-подобных подсистем (`.Core`/`.Runtime`/`.Workspace`)
      + пометку статуса (standalone, потребитель — devstand).
- [ ] B. Подключение к `Mars.WebApp`: сейчас модуль НЕ собран в корне композиции, единственный
      потребитель — стенд `devstands/StandPxBlocksApp`. Решение: подключить `AddPxBlocks()`/
      `UsePxBlocks()` в `MarsWebAppStartup` или зафиксировать standalone-статус до появления
      реальных контекстов (админка/ноды).
- [ ] C. NuGet: CI (`nuget-publish.yml`) пакует все `src/**` с `<PackageId>`; он объявлен у
      Core/Runtime/Abstractions/Contracts → на следующем теге опубликуются 4 пакета
      `mdimai666.Mars.PxBlocks.*`. Решить, готовы ли; у Host и Workspace PackageId нет —
      подтвердить, что это намеренно.

### Фичи

- Rich-редакторы полей: клик по полю → Blazor-форма (оверлей), удобно для объектов с множеством полей.
- Фишки PXT поштучно из `pxtblocks/*` (flyout-hover, workspace search и т.п.;
  редактор полей-аргументов функций уже портирован в 14C).
- Модал-вариант редактора; настраиваемый префикс маршрута в `PxServerRunClient`.
- Реальные контексты под админку (LINQ-фильтры, порядок операций в нодах);
  доменные пакеты под устройства (MQTT / ноды Mars.Nodes) и запуск PxBlocks-программ
  из графа Mars.Nodes (нода-обёртка).

## Архитектура: гибридная обёртка Blockly

**Blockly не переписывать, PXT целиком не подключать.**

- Движок — официальный **blockly 13.1.1** (тот же пин, что у PXT) в браузере: рендеринг,
  drag&drop, стыковка, flyout, undo/redo, trashcan — всё Blockly.
- Blazor — оболочка: хром редактора, конфигурация (toolbox/типы/определения блоков), сериализация.
- Свой SVG-рендерер в Blazor не пишется (5 прошлых попыток провалились именно на этом).
- Мост: TypeScript, сборка Vite в `wwwroot/dist` (паттерн `EditorJsBlazored` из этого репо).
- PXT целиком не подключается; его слой `pxtblocks/*` переносится пофайлово
  (референс: `C:\js\2026\microsoft-pxt`, при переносе заменять `pxt.*`-зависимости).

### Почему не PXT целиком

- PXT = целое приложение: React 17 оболочка, TS-компилятор в браузере, симулятор,
  пакеты/облако/тулториалы, сборка jake/gulp + свой CLI. Всё это не нужно.
- `pxtblocks/*` сцеплены с рантаймом PXT (`pxt.*`, `pxtlib.d.ts`, `lf()`) — целиком
  переносятся только вместе с потрохами PXT.
- Подключение «как есть» = iframe (чёрный ящик, свои блоки/типы не добавить)
  или форк с таргетом (поддержка гигантской сборки ради нескольких файлов). Тупик.

### Что взято из PXT

- **Рендерер «pxt»** — порт `pxtblocks/plugins/renderer/{renderer,constants,pathObject,info,drawer}.ts`
  (`extends Blockly.zelos.*`, регистрация `pxt`); `shapeFor()` дополнен чтением форм из реестра типов.
- **Формы стыковок по типам**: Boolean → HEXAGONAL (шестиугольник), Number/String → ROUNDED,
  statement-коннекторы → NOTCH.
- **Редактор функций** (14C) — порт `pxtblocks/plugins/functions` + `fields/fieldArgumentEditor.ts`.
- **Кастомный ConnectionChecker** — по образцу `plugins/duplicateOnDrag/connectionChecker.ts`.

### Система типов

- Канонический реестр типов `PxType` живёт в C# (`Mars.PxBlocks.Core`): имя, форма,
  правила совместимости/подтипов (`CompatibleWith`, в т.ч. `"*"`). В JS при старте уходит
  сериализованная матрица.
- JS-сторона: `check` на коннекторах + `PxConnectionChecker extends Blockly.ConnectionChecker`
  (зарегистрирован как `pxt`, включён опцией `plugins: { connectionChecker: 'pxt' }`)
  + расширение `shapeFor()` на наши типы (Object/Array → квадрат).
- Поведение «bool-шестиугольник не стыкуется с number» обеспечивается checker'ом,
  форма — визуальное следствие.

### Исполнение блоков — «вариант C» (2026-08-14)

- **Tree-walking интерпретатор в .NET** (`Mars.PxBlocks.Runtime`, чистый .NET без JS —
  работает и в WASM). workspace JSON → AST (`PxParser`) → `PxInterpreter`.
  Без кодогенерации и Roslyn: песочница «по построению» (бек исполняет пользовательские
  программы), лимиты шагов, ошибки/подсветка маппятся на blockId.
- **Отклонены**: кодоген + Roslyn (песочница на сервере, нет в WASM, маппинг ошибок на блоки);
  «`BlockImplement` на каждый блок» (control flow размазывается по плагинам, теряются единые
  гарантии скоупов/break/short-circuit); Jint + JS-кодоген Blockly (чужая JS-семантика,
  конфликт с решением «декларативное исполнение в .NET»).
- **Control flow — в ядре интерпретатора, не плагинится**: последовательности, if/else,
  циклы, break/continue, процедуры и функции, `variables_get/set`, short-circuit `logic_operation`.
- **Листья — `IPxBlockImplement` по TypeId** (аналог `INodeImplement<TNode>`),
  регистрация локатором `RegisterAssembly` (по образцу `NodesLocator`).
- Все шаги `ValueTask` (задел на паузы/сенсоры); события исполнения с blockId
  (BlockEntered/Exited/Output — задел на подсветку и отладчик).
- Событийные блоки **Start/Loop — аналог Arduino setup()/loop()** (2026-08-15): хат-блоки
  без prev/next; фазы запуска задаёт `EventNames` (null — все верхнеуровневые стеки,
  пустой список — ничего, список имён — фазы в порядке списка; Loop всегда после всех).

### Серверное исполнение и встраивание (решения Этапов 8–11)

- Определения блоков и реализации объявляются ТОЛЬКО на сервере; редактор получает их через
  `api/PxBlocks` (Contexts/{имя}; Definitions — запасной «всё сразу»). Удалённое определение →
  блок «Unknown» на полотне, запуск блокируется серверным разбором (PxParseException с blockId).
- **Голого хранилища сценариев нет** (2026-08-16): JSON блоков хранят владельцы (ноды/админка)
  и передают в Run напрямую; запуск по месту — через `IPxRunManager` из DI.
- **REST запуска объявляет хост** (Этап 10): библиотека даёт только read-эндпоинты редактора;
  маршрут должен совпадать с ожиданием `PxServerRunClient` — `POST api/PxBlocks/Run`
  (PxRunRequest → PxRunResponse) и `POST api/PxBlocks/Stop/{runId:guid}` → bool.
  Образцы: `PxRunController` стенда и `TestPxRunController` сквозных тестов.
- **Режим запуска решает контекст, не редактор** (2026-08-16): `PxEditorContext`
  (fluent `PxEditorContext.Define("имя")…`) — наборы блоков, toolbox, политика
  (EventNames/StepLimit/OutputLimit), фильтр событийных блоков. Явные поля запроса
  в приоритете над политикой контекста.
- **Состояние запуска — «Путь 1»** (2026-08-17): своей сущности «environment» у PxBlocks НЕТ —
  только канал для объекта хоста. Имплементации блоков НЕ синглтоны (в отличие от Mars.Nodes —
  там синглтоны замысел): создаются В МОМЕНТ ЗАПУСКА, по экземпляру на исполнение
  (`PxContext.Implement(typeId)`, лениво); состояние запуска допустимо держать в полях.
  `IPxRunManager.Start(request, state)` принимает state во владение и диспозит по завершении
  (при Started=false — сразу); хост, передавший state, сам его НЕ диспозит.
  «Путь 2» (фабрика состояния у контекста) закрыт не делая: REST запуска принадлежит хосту,
  механизм в PxBlocks был бы дублированием.
- Общие для разных типов блоков объекты — состояние запуска; данные, видимые пользователю, —
  переменные Blockly (PxValue).

### Конвенция typeId (2026-08-17)

Трёхуровневые имена `уровень.категория.имя`:

- **`core.категория.имя`** — встроенные блоки библиотеки: события `core.events.start/loop`,
  стандартные категории языка `core.logic.*`, `core.loops.*`, `core.math.*`, `core.text.*`,
  `core.variables.get/set/change` (определения — `PxEventBlocks`/`PxStandardBlocks` в Core,
  исполнение — ядро PxInterpreter/PxParser + листья `Standard/`; сервер отдаёт их в
  КАЖДЫЙ контекст — редактор не зависит от встроенных определений Blockly).
- **`пакет.категория.имя`** — блоки хостов: у стенда `demostand.demo.*` и `demostand.playwright.*`.
- Исключение (фаза 2): процедуры `procedures_*` — Blockly-имена, сохранены только ради
  совместимости сохранённых сценариев; "Functions" с Этапа 14C — редактор функций MakeCode
  (порт pxtblocks/plugins/functions в JsSrc/functions/: function_definition/call(_output)/
  argument_reporter_*/function_return с +/−, диалог «Edit Function» без React, flyout-
  колбэк PROCEDURE — «Make a Function...»); для "Variables" свой flyout-колбэк, т.к.
  штатный хардкодит variables_get/set.
- Массивы — набор MakeCode, 0-based (Этап 14B): create_empty/create_with/repeat/length —
  встроенные Blockly с лейблами MakeCode (оверрайд Blockly.Msg в JsSrc/index.ts; мутатор
  create_with задаётся в init, серверным JSON не выразим), lists_index_get/
  lists_index_set/array_indexof — серверные определения PxStandardBlocks; исполнение —
  StdLists. Имена мутаторов (controls_if_mutator…) — штатные Blockly, не typeId.
- Функции (Этап 14C): параметры в определении/декларации inline сразу после имени
  (reorder входов как в commonFunctionMixin PXT); удаление и перемещение параметров
  в диалоге «Edit Function» — иконками в виджете поля `field_argument_editor` (порт
  поля PXT + стрелки вверх/вниз); if-return — свой `core.functions.if_return` (штатный
  `procedures_ifreturn` вне `procedures_def*` вешает warning и disable), return во
  flyout — с тенью null.
- Старые имена (`px_start`, `text_print`…) в исторических документах — до конвенции.

## Состав

Реструктуризация 2026-09-10: семейство приведено к конвенциям решения
(`Mars.PxBlocks.Shared` → `Core`, `Host.Shared` разделён на `Contracts` + `Abstractions`).

```
src/Mars.PxBlocks/
├─ Mars.PxBlocks.Core/            # модели без JS: определения блоков (fluent PxMaster), реестр типов, toolbox
├─ Mars.PxBlocks.Runtime/         # исполнение: AST + tree-walking интерпретатор
├─ Mars.PxBlocks.Contracts/       # wire-DTO, клиент API/хаба, транспорт событий (WASM-безопасно)
├─ Mars.PxBlocks.Abstractions/    # серверные контракты: каталог блоков, менеджер запусков, контексты
├─ Mars.PxBlocks.Host/            # серверное исполнение: api/PxBlocks (read) + SignalR-хаб
└─ Mars.PxBlocks.Workspace/       # RCL-редактор
   ├─ JsSrc/                      # TypeScript (наши + порты из pxtblocks): index.ts, renderer/,
   │                              # functions/, extensions/, dialogs.ts, railDelete.ts, connectionChecker.ts
   ├─ package.json / vite.config.js / tsconfig.json / copy-media.mjs
   ├─ wwwroot/dist/               # артефакт Vite (ESM, коммитится) + media/ из blockly
   ├─ wwwroot/pxblocks.css        # хром редактора (подключается хостом link-ом в head)
   ├─ PxBlocksWorkspace.razor     # полотно (inject + примитивы Save/Load/Undo)
   ├─ PxBlocksEditor.razor        # чистая форма редактирования
   ├─ PxSandboxEditor.razor       # браузерная песочница (запуск)
   ├─ PxToolboxRail.razor         # рейка категорий в стиле MakeCode
   ├─ e2e/                        # headless-проверки стенда (playwright, msedge)
   └─ PxWorkspaceJsInterop.cs     # ESM-загрузка и вызовы JS
tests/Mars.PxBlocks.Tests/        # xunit.v3 + Microsoft.Testing.Platform (exe)
devstands/StandPxBlocksApp/       # стенд: Blazor Web App + WASM (песочница, форма, /browser)
```

### `Mars.PxBlocks.Core` — модели без JS

- `Toolbox/` — `PxToolbox` + `PxToolboxCategory`/`PxToolboxSeparator`/`PxToolboxBlock`;
  `ToJson()` → toolbox JSON Blockly (`custom: VARIABLE/PROCEDURE` — динамические категории).
- `Types/` — `PxType`/`PxShape`/`PxTypeRegistry`: канонические типы стыковок
  (форма по типу, матрица совместимости — см. «Система типов»).
- `Definitions/` — `PxBlockDefinition` (`ToJson()` → Blockly JSON: messageN/argsN, output или
  previous/next statement, extensions, mutator). Объявляются fluent-API
  `PxMaster.Define("id").Message("текст {arg}", PxMaster.Number("arg"))`, группируются
  классами `PxBlockSet` по областям (аналог пакетов PXT, см. `PxDemoBlocks`); наследование —
  только для блоков с динамической структурой. Плейсхолдеры в сообщениях: именованные `{имя}`
  (порядок аргументов выводится из строки, %1..%N подставляются сами) или позициянные `%1..%N`.
  Аргументы и фабрики: `PxFieldNumber`/`PxMaster.Number`, `PxFieldText`/`PxMaster.Text`,
  `PxFieldDropdown`/`PxMaster.Dropdown`, `PxFieldVariable`/`PxMaster.Variable` (field_variable),
  `PxValueInput`/`PxMaster.Value`, `PxStatementInput`/`PxMaster.Do` (входы с `Check`).
  Блокам с несколькими value-входами в одну строку — `.Inline()` (inputsInline).
- `PxEventBlocks` (core.events.start/loop; фабрики CreateStart/CreateLoop) и `PxStandardBlocks`
  (все core.* категории: логика/циклы (в т.ч. pause)/математика (в т.ч. min_max и map)/текст
  (в т.ч. расширения MakeCode: substring/includes/compare/split/parse/char_code)/массивы
  (lists_index_get/set, array_indexof; остальные — встроенные Blockly)/переменные (get/set/change)
  + досрочный `procedures_return` и `core.functions.if_return`; мутаторы — штатные Blockly:
  controls_if_mutator, text_join_mutator, math_is_divisibleby_mutator, text_charAt_mutator —
  имена сверять с blocks_compressed.js) — базовые наборы определений каждого контекста.
  Блоки с мутаторами, у которых хелпер строит входы (text_join), объявляются с пустым сообщением.
- `PxBlocklyEvent` (пакет событий из JS), `PxWorkspaceState`.

### `Mars.PxBlocks.Runtime` — исполнение (AST + интерпретатор)

Чистый .NET без JS (работает и в WASM): `Values/` — иерархия `PxValue` (Number/Boolean/
String/Object/List/Null); `Ast/` — узлы программы (каждый несёт blockId); `Parsing/` —
`PxParser` (Blockly JSON → AST; неизвестный лист → ошибка с blockId; форматы сверены
с blockly 13.1.1) + `PxCoreBlocks` (структурные typeId, синхронизированы с PxStandardBlocks);
`Execution/` — `PxInterpreter` (control flow в ядре), `PxContext`, `IPxBlockImplement` +
`PxBlockImplementsLocator`; `Standard/` — имплементации стандартных листьев core.*
(математика, логика, текст, массивы — `StdLists`, `core.text.print`); функции `function_*` —
тоже в ядре интерпретатора (рамка скоупа, параметры по id/имени, `PxReturnSignal`).
Точки входа: `PxParser.CreateDefault()`, `PxInterpreter.CreateDefaultImplements()`.

Жизненный цикл имплементаций и состояние запуска (Этап 11, Путь 1 — см. решения выше):
- Локатор хранит ТИПЫ; экземпляры создаются В МОМЕНТ ЗАПУСКА — по экземпляру на исполнение
  (`PxContext.Implement(typeId)`, лениво). Состояние запуска допустимо держать в полях.
- Состояние запуска (`PxContext.State`, объект хоста — браузер, соединение, сервис…)
  попадает в имплементации конструктором (один параметр, совместимый с типом состояния)
  или через `PxContext.GetState<T>()`. В `PxRunOptions.State` его кладёт запускающий.
- Начальные переменные: `PxRunRequest.InitialVariables` (имя → JSON-значение) →
  `PxValueJson.FromJson` → `PxContext` перезаписывает объявленные переменные ПО ИМЕНИ,
  неизвестные имена игнорируются; ошибка конвертации — Started=false сразу.

### `Mars.PxBlocks.Workspace` — RCL-редактор

- `JsSrc/` — TypeScript, сборка Vite в `wwwroot/dist/PxBlocks.js` (ESM, коммитится вместе
  с `wwwroot/media/`; загрузка через `import("./_content/Mars.PxBlocks.Workspace/dist/PxBlocks.js?v=…")`,
  версия в query — cache-busting, как в AiChatAssets/MarsCodeEditor2):
  - `index.ts` — `injectWorkspace` (прячет нативное меню категорий inline + resize),
    `updateToolbox`, `selectCategory`/`clearToolboxSelection`/`isFlyoutVisible` (flyout
    из Blazor-рейки), `setTypes`, `registerBlockDefinitions`,
    `saveWorkspace`/`loadWorkspace`/`clearWorkspace`/`undo`, `registerEvents`
    (синхронизирует выбор рейки событием `TOOLBOX_ITEM_SELECT`);
  - `renderer/` — порт рендерера «pxt» из PXT (9 файлов);
  - `connectionChecker.ts`, `extensions/objectBuilder.ts` (mutator "create object":
    «+» добавляет пары field→value, состояние через saveExtraState/loadExtraState),
    `extensions/hat.ts` (`px_hat_cap`), `functions/` (порт редактора функций PXT);
  - `dialogs.ts` — окна модуля вместо нативных окон Blockly (prompt/confirm/alert):
    создание и переименование переменной, удаление переменной, предупреждения;
  - `railDelete.ts` — рейка категорий зарегистрирована delete-областью Blockly
    (DRAG_TARGET + DELETE_AREA): бросок блока на рейку удаляет его, как в MakeCode,
    при наведении рейка краснеет и показывает корзинку (класс `pxb-rail-drop`).
- `PxBlocksWorkspace.razor` — **полотно**: inject, параметры `OptionsJson`/`Toolbox`/`Types`/
  `BlockDefinitions`, события `OnReady`/`OnWorkspaceChanged`, примитивы `SaveAsync`/`LoadAsync`/
  `ClearAsync`/`UndoAsync`/`RedoAsync`.
- `PxBlocksEditor.razor` — **чистая форма редактирования** (Этап 9): рейка + полотно +
  поиск, API Save/Load/Undo/Redo/Clear/Center/Highlight; БЕЗ тулбара и БЕЗ запуска.
  Управляемый режим: параметр `BlocksJson` (смена значения перечитывает полотно),
  сохранение — `SaveAsync()` хостом. Контекстный режим: `Context` (имя) + `Transport`
  (IPxBlocksApiClient) — определения и toolbox из `api/PxBlocks/Contexts/{имя}`.
  Параметр `RailDisplay` (Auto/Full/Compact) — сворачивание рейки, см. PxToolboxRail.
  Хост вставляет `<PxBlocksEditor />` + link на pxblocks.css в head.
- `PxSandboxEditor.razor` — **браузерная песочница**: тулбар Undo/Redo/Clear/Center/Run/Stop
  + статус, панель вывода, автосейв в localStorage (Blazored.LocalStorage, ключ — `StorageKey`),
  исполнение in-process или на сервере (`RunTransport`, `EventNames`). Единственный редактор,
  запускающий полный JSON программы из браузера.
- `PxToolboxRail.razor` — рейка категорий в стиле MakeCode: иконки (inline SVG), поиск
  (дебаунс 250 мс, временная flyout-категория "Search"), экспандер Advanced; выбранная
  категория заливается своим цветом; клик по выбранной закрывает flyout. Компактный
  режим (CSS, `data-rail` на корне редактора): Auto — container queries, при ширине
  ≤560px только иконки + поиск-кнопка со всплывающим полем; Compact — то же всегда.
- `wwwroot/pxblocks.css` — хром редактора: рейка, тёмный flyout, заголовки
  (`blocklyFlyoutHeading`), скрытие нативного меню категорий (`display:none !important` —
  `Toolbox.init()` ставит inline `display:block`). Подключается хостом link-ом в head
  (стенд — `App.razor`), НЕ инъекцией из скрипта.
- `PxWorkspaceJsInterop.cs` — ленивая загрузка ESM-модуля и вызовы JS.
- `e2e/check.mjs` — headless-проверки стенда системным Edge (playwright, `channel: 'msedge'`):
  замеры ширины svg/контейнеров + скриншоты; `check-browser.mjs` — страница `/browser`
  (загрузить пример, Run, дождаться вывода; реально открывает серверный Edge с Википедией);
  `check-flyouts.mjs` — flyout-ы стандартных категорий; `check-functions.mjs` — диалог функций.
- npm-инфраструктура: `package.json` (blockly 13.1.1), `vite.config.js` (lib → ESM),
  `tsconfig.json`, `copy-media.mjs` (media blockly → wwwroot/media).

### `Contracts` + `Abstractions` + `Host` — серверное исполнение

- `Mars.PxBlocks.Contracts` (WASM-безопасно) — wire-DTO и клиентские контракты: DTO
  (`PxRunRequest` с клиентским RunId и `ContextName`, `PxRunResponse`, `PxRunResultDto`,
  `PxDefinitionsResponse`, `PxEditorContextInfo`), `IPxBlocksApiClient`, `IPxRunTransport`,
  `IPxBlocksClient` (типизированный хаб), константы (маршрут `/_ws/pxblocks`, группа `pxblocks`).
- `Mars.PxBlocks.Abstractions` (только сервер) — контракты исполнения: `IPxRunManager`,
  `IPxBlockCatalog`, `IPxBlocksBroadcaster`, `IPxEditorContextRegistry`, `PxEditorContext`.
- `Mars.PxBlocks.Host` — `PxBlockCatalog` (определения + локатор имплементаций; toolbox =
  дефолт + доменные категории), `PxRunManager`+`PxRunSession` (разбор синхронно, исполнение
  фоном, события пакетируются 100 мс/256 и стримятся цепочкой последовательных отправок —
  порядок RunEvents→RunFinished гарантирован; политика из контекста — явные поля запроса
  в приоритете), `PxBlocksHub` (авто-вход в группу при подключении), `PxBlocksController`
  (api/PxBlocks — ТОЛЬКО read-эндпоинты редактора: Definitions, Contexts, Contexts/{имя}),
  `MainPxBlocks.AddPxBlocks/UsePxBlocks` (UsePxBlocks — ядерные PxEventBlocks; доменные
  сборки и контексты регистрирует хост).
- **REST запуска объявляет хост** (см. решения выше).

### Стенд и тесты

- `devstands/StandPxBlocksApp` — Blazor Web App + WASM для проверки редактора;
  `/` = `<PxSandboxEditor Context="sandbox" />` (определения и политика запуска из
  контекста, запуск — через `Controllers/PxRunController` стенда), `/form` =
  `<PxBlocksEditor Context="demo" />` (управляемая форма, «хранилище» в памяти
  страницы, переключатель RailDisplay и слайдер ширины), `/browser` =
  `<PxSandboxEditor Context="browser" />` (браузерные скрипты + кнопка «Пример»,
  грузит сценарий с `GET api/PxBlocks/Samples/browser`). Контексты «sandbox»,
  «demo» и «browser» регистрируются в Program.cs. Пререндер отключён глобально
  в App.razor. `ILocalStorageService` регистрируется и в серверном `Program.cs`
  (нужно для пререндера), и в клиентском.
- **Браузерные скрипты** (контекст «browser», Этап 12): сценарии Playwright исполняются
  НА СЕРВЕРЕ в системном Edge (`channel: msedge`, видимое окно, SlowMo 50 — как
  Mars.E2E.Tests). Домен `Blocks/Browser/` в серверной сборке стенда: `PxBrowserBlocks`
  (demostand.playwright.goto/click/type/press/wait_selector/wait_ms/get_text/eval_js/
  print_texts), имплементации с инъекцией `PxBrowserRunState` конструктором (ленивый запуск
  браузера, `IAsyncDisposable` — диспозит PxRunManager), свой toolbox без Loop (событийные
  блоки контекста фильтруются `PxEditorContext.EventBlocks`), только событие Start.
  Селекторы/тексты — value-входы String c shadow-блоками из тулбокса
  (`PxToolboxBlock.InputsJson`). Состояние запуска создаёт PxRunController по
  `request.ContextName`.
- `tests/Mars.PxBlocks.Tests` — xunit.v3 + Microsoft.Testing.Platform (`OutputType=Exe`):
  сериализация toolbox, реестра типов, определений блоков; интерпретатор; серверный
  запуск (реальный Kestrel + SignalR-хаб через штатный PxServerRunClient).

## Вес и сборка (факты из node_modules blockly 13.1.1)

| Что | Размер |
|---|---|
| `blockly.min.js` (core+блоки+en) | 776 КБ, gzip ~230–250 КБ |
| `blocks_compressed.js` (все стандартные блоки) | 496 КБ — берём только нужные |
| генераторы js/python/lua/php/dart | **не нужны** — в бандл не попадают |
| `media/` (иконки, звуки) | 19 КБ — копия в wwwroot |

- Загрузка ленивая (при открытии редактора) — страницы Mars не утяжеляет.
- Лицензия Blockly Apache-2.0 — совместима (атрибуция).

## Сборка и запуск

```
# JS-бандл (после правок JsSrc):
cd src/Mars.PxBlocks/Mars.PxBlocks.Workspace
npm install          # один раз
npx tsc --noEmit     # проверка типов
npm run build        # Vite → wwwroot/dist + media

# Стенд:
dotnet run --project devstands/StandPxBlocksApp/StandPxBlocksApp/StandPxBlocksApp

# Тесты (xunit.v3/MTP-проект собирается в exe; на SDK 10 путь `dotnet test` заблокирован,
# корневой test-all.ps1 запускает exe-файлы напрямую):
tests/Mars.PxBlocks.Tests/bin/Debug/net10.0/Mars.PxBlocks.Tests.exe
```

## Грабли (проверено на практике)

1. **Toolbox должен существовать с момента inject** (можно пустой) — иначе
   `workspace.updateToolbox` бросает «Existing toolbox is null».
2. **Обычные расширения Blockly (`Extensions.register`) не могут менять mutator-свойства**
   (`saveExtraState`/`loadExtraState`) — ошибка «mutation properties changed». Для блоков с
   динамической структурой: `Extensions.registerMutator` + поле `mutator` в JSON-определении.
3. **Razor: строковый параметр без `@` передаётся литералом** — нужно `OptionsJson="@OptionsJson"`,
   иначе в JS улетит строка `"OptionsJson"`. Предупреждения компилятора для свойств не будет.
   Наступили повторно (2026-08-17): `Context="Context"` в PxSandboxEditor передавал
   литерал «Context» вместо имени контекста — определения грузились с 404. Для
   нестроковых параметров (объекты/enum/EventCallback) значение, наоборот, читается
   как C#-выражение, поэтому `Toolbox="Toolbox"` работает и без `@`.
4. Запущенный стенд **держит DLL** — перед `dotnet build` остановить `dotnet run`.
   Если `_framework/*` отвечает 500, а в логе «Static Web Assets are not enabled» —
   артефакты сборки рассинхронизированы: остановить сервер и пересобрать.
5. Инкрементальная сборка Client может увидеть старую сборку Workspace RCL (Razor-генератор) —
   при странных ошибках привязки параметров пересобирать с `--no-incremental`.
6. Пин blockly не поднимать без нужды: порты из `pxtblocks/*` написаны под 13.1.1.
7. События Blockly → .NET идут пакетами (~200 мс debounce), UI-события фильтруются;
   автосейв в стенде/редакторе — на каждый пакет.
8. Цветовые хаки PXT из `pxtblocks/plugins/renderer/pathObject.ts` (override `applyColour`)
   не переносим: в PXT там битый hex `#0000000` (форк pxt-blockly терпел, официальный
   `blend()` вернул бы `null` → `stroke="null"` → контур исчезал), а сами хаки — blend 0.6
   к чёрному/белому по контрасту и высветление shadow — на нашей палитре дают «чёрную
   ручку» и светлое «гало». Контур считаем как официальный Zelos: `colourTertiary` =
   `blend('#000', primary, 0.25)` (тональное затемнение с сохранением тона), у shadow —
   tertiary родителя. Любую цветовую логику из pxtblocks сверять с официальным blockly.
9. CSS редактора (`pxblocks.css`) подключается хостом **link-ом в head** (стенд — `App.razor`),
   не инъекцией из скрипта: поздняя загрузка CSS сдвигает лейаут после `Blockly.inject`,
   и полотно остаётся неверной ширины до первого ресайза окна. Нативное меню категорий
   прятать только с `!important` (Toolbox.init ставит inline `display:block`).
10. **Шапку хат-блока нельзя задавать `style.hat` в JSON определения**: `jsonInit` Blockly
    читает `style.hat` один раз и обнуляет `style` прямо в общем объекте определения —
    шапка достаётся только первому созданному экземпляру блока (flyout → drag → flyout
    теряют шапку). Используем расширение `px_hat_cap` (`JsSrc/extensions/hat.ts`):
    `PxBlockDefinition.Hat` генерирует `extensions: ["px_hat_cap"]`.

## История этапов (сжато)

Подробности шагов и счётчики тестов — `git show ea65cf08:src/Mars.PxBlocks/Mars.PxBlocks.Workspace/PLAN.md`.

- **0–4** — фундамент (Vite+blockly 13.1.1, inject, стенд), порт рендерера «pxt»,
  toolbox и базовые блоки, полный редактор (save/load/undo/события пакетированием),
  система типов (реестр в C# → матрица в JS, ConnectionChecker).
- **5 / 5.1** — определения блоков и mutator "create object"; fluent-API `PxMaster` +
  `PxBlockSet` + именованные плейсхолдеры `{имя}` (решение 2026-08-13, «вариант B»;
  фасад назван PxMaster — короткое «Px» пользователь счёл слишком куцым).
- **6** — тулбокс в стиле MakeCode: рейка — аналог React-шелла `webapp/src/toolbox.tsx`
  (в pxtblocks/Blockly этого нет), flyout — Blockly + CSS шелла (`theme/blockly-core.less`).
- **7** — исполнение в .NET: вариант C (см. «Архитектура»); 2026-08-15 — событийные блоки
  Start/Loop (Arduino setup/loop), фикс шапки через `px_hat_cap`.
- **8** — запуск на сервере (2026-08-15): Host (каталог, PxRunManager+PxRunSession, хаб,
  контроллер), определения только на сервере, браузерный рантайм сохранён как задел;
  неизвестные блоки — серый placeholder «Unknown: тип».
- **9** — встраиваемость (2026-08-16): контексты `PxEditorContext`, семейство редакторов
  (Workspace/Editor/SandboxEditor), компактная рейка `PxRailDisplay` (container queries,
  порог 560px). Исходный промпт — `ai/Prompts/PxBlocksEmbedInNodeEditorPrompt.md`.
- **10** — чистое встраивание (2026-08-16): REST запуска объявляет хост, у библиотеки
  только read-эндпоинты; песочница стала контекстной.
- **11** — состояние запуска, Путь 1 (2026-08-17): имплементации по экземпляру на запуск,
  `Start(request, state)` с владением и диспоузом; `InitialVariables` в запросе.
- **12** — браузерные скрипты (2026-08-17): Playwright-контекст «browser» стенда,
  исполнение на сервере в системном Edge.
- **13** — 13A: английские лейблы блоков/категорий (стандартные Blockly/MakeCode);
  страницы стенда, комментарии и документация — по-русски (решение пользователя);
  typeId/поля/значения дропдаунов не менялись — сохранённые сценарии совместимы.
  13B: `procedures_return`. 13C: `core.variables.change`, `core.loops.pause`, `core.math.min_max`.
- **14** — 14A: расширения текста MakeCode (substring/includes/compare/split/parse/char_code)
  + `core.math.map`; 14B: массивы — ПОЛНЫЙ набор MakeCode (19 блоков), 0-based; sort в общий
  набор MakeCode НЕ входит (arraySort без block-аннотации) — не добавлен сознательно;
  14C: редактор функций — порт pxtblocks/plugins/functions, диалог «Edit Function» без React,
  перемещение параметров стрелками — наше дополнение (в PXT не было).

### Что сделано со старым кодом (до Этапа 1)

Удалены (свой рендеринг, заменён Blockly): PxWorkspace.razor, PxBlockComponent.razor,
PxBlockSvgHelper.cs, PxToolbox.razor, pxWorkspaceJs.js, PxBlock.cs, PxField.cs, PxInput.cs —
всё остаётся в git-истории. Переиспользованы: PxBlockDefinition, PxToolboxCategory,
PxWorkspaceState как конфиг-модели.

## Риски

- Портируемые файлы pxtblocks написаны под blockly своей эпохи — пинним ту же версию
  13.1.1, что у PXT; точечные `pxt.*`-зависимости заменяем при переносе.
- Точки сцепления с внутренностями Blockly: checker, shapeFor, свои поля/мутаторы —
  держим в отдельных модулях JsSrc, чтобы переживать апгрейды Blockly.
- Поток событий на больших схемах — пакетирование заложено (Этап 3).
- Контракт парсера — Blockly JSON версии blockly 13.1.1, включая extraState мутаторов:
  фиксируем фикстурами в тестах, чтобы переживать апгрейды Blockly.
