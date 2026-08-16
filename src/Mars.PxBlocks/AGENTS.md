# Mars.PxBlocks — инструкция агенту

PxBlocks — визуальный редактор блоков в духе Microsoft PXT (MakeCode/Blockly) на Blazor.
Этапы 0–6 (редактор), 7 (исполнение в .NET), 8 (запуск на сервере — Mars.PxBlocks.Host),
9 (встраиваемость: контексты, песочница/форма), 10 (чистое встраивание: REST запуска
объявляет хост, у библиотеки только read-эндпоинты), 11 (состояние запуска, имплементации
по запуску), 12 (браузерные скрипты — Playwright-контекст стенда) готовы. Без симулятора
и без кодогенерации. Детальный план — `Mars.PxBlocks.Workspace/PLAN.md`. Цели: `Mars.PxBlocks.Workspace/Mission.md`.

## Архитектура: гибридная обёртка Blockly

- Движок — официальный **blockly 13.1.1** (тот же пин, что у PXT) в браузере: рендеринг,
  drag&drop, стыковка, flyout, undo/redo, trashcan — всё Blockly.
- Blazor — оболочка: хром редактора, конфигурация (toolbox/типы/определения блоков), сериализация.
- Свой SVG-рендерер в Blazor не пишется (5 прошлых попыток провалились именно на этом).
- PXT целиком не подключается; его слой `pxtblocks/*` переносится пофайлово
  (референс: `C:\js\2026\microsoft-pxt`, при переносе заменять `pxt.*`-зависимости).

## Конвенция typeId (2026-08-17)

Трёхуровневые имена `уровень.категория.имя`:
- **`core.категория.имя`** — встроенные блоки библиотеки: события `core.events.start/loop`,
  стандартные категории языка `core.logic.*`, `core.loops.*`, `core.math.*`, `core.text.*`,
  `core.variables.get/set` (определения — `PxEventBlocks`/`PxStandardBlocks` в Shared,
  исполнение — ядро PxInterpreter/PxParser + листья `Standard/`; сервер отдаёт их в
  КАЖДЫЙ контекст — редактор не зависит от встроенных определений Blockly).
- **`пакет.категория.имя`** — блоки хостов: у стенда `demostand.demo.*` и
  `demostand.playwright.*`.
- Исключения (фаза 2): процедуры `procedures_*` и массивы `lists_*` — пока Blockly-имена
  (динамический тулбокс «Функции» и механика Blockly; для «Переменных» свой flyout-колбэк
  в JsSrc/index.ts, т.к. штатный хардкодит variables_get/set). Имена мутаторов
  (controls_if_mutator…) — штатные Blockly, не typeId.

## Состав

### `src/Mars.PxBlocks/Mars.PxBlocks.Shared` — модели без JS
- `Toolbox/` — `PxToolbox` + `PxToolboxCategory`/`PxToolboxSeparator`/`PxToolboxBlock`;
  `ToJson()` → toolbox JSON Blockly (`custom: VARIABLE/PROCEDURE` — динамические категории).
- `Types/` — `PxType`/`PxShape`/`PxTypeRegistry`: канонические типы стыковок.
  Форма по типу: Boolean → шестиугольник, Number/String → скругление, Object → квадрат;
  `CompatibleWith` (в т.ч. `"*"`) — матрица совместимости.
- `Definitions/` — определения блоков: `PxBlockDefinition` (`ToJson()` → Blockly JSON
  definitions: messageN/argsN, output или previous/next statement, extensions, mutator).
  Объявляются fluent-API `PxMaster.Define("id").Message("текст {arg}", PxMaster.Number("arg"))`,
  группируются классами `PxBlockSet` по областям (аналог пакетов PXT, см. `PxDemoBlocks`);
  наследование — только для блоков с динамической структурой. Плейсхолдеры в сообщениях:
  именованные `{имя}` (порядок аргументов выводится из строки, %1..%N подставляются сами)
  или позициянные `%1..%N`. Аргументы и фабрики: `PxFieldNumber`/`PxMaster.Number`,
  `PxFieldText`/`PxMaster.Text`, `PxFieldDropdown`/`PxMaster.Dropdown`,
  `PxFieldVariable`/`PxMaster.Variable` (field_variable), `PxValueInput`/`PxMaster.Value`,
  `PxStatementInput`/`PxMaster.Do` (входы с `Check`). Блокам с несколькими value-входами
  в одну строку — `.Inline()` (inputsInline; без него Blockly складывает входы в столбик).
- `PxEventBlocks` (core.events.start/loop; фабрики CreateStart/CreateLoop) и
  `PxStandardBlocks` (все core.* категории: логика/циклы/математика/текст/переменные;
  мутаторы — штатные Blockly: controls_if_mutator, text_join_mutator,
  math_is_divisibleby_mutator, text_charAt_mutator — имена сверять с blocks_compressed.js) —
  базовые наборы определений каждого контекста. Блоки с мутаторами, у которых хелпер
  строит входы (text_join), объявляются с пустым сообщением.
- `PxBlocklyEvent` (пакет событий из JS), `PxWorkspaceState`.

### `src/Mars.PxBlocks/Mars.PxBlocks.Runtime` — исполнение (AST + интерпретатор)
Чистый .NET без JS (работает и в WASM): `Values/` — иерархия `PxValue` (Number/Boolean/
String/Object/List/Null); `Ast/` — узлы программы (каждый несёт blockId); `Parsing/` —
`PxParser` (Blockly JSON → AST; неизвестный лист → ошибка с blockId; форматы сверены
с blockly 13.1.1) + `PxCoreBlocks` (структурные typeId, синхронизированы с PxStandardBlocks);
`Execution/` — `PxInterpreter` (control flow в ядре: if/циклы/break/процедуры/переменные/
short-circuit; лимит шагов; события BlockEntered/Exited/Output), `PxContext`,
`IPxBlockImplement` + `PxBlockImplementsLocator`; `Standard/` — имплементации стандартных
листьев core.* (математика, логика, текст, `core.text.print`).
Точки входа: `PxParser.CreateDefault()`, `PxInterpreter.CreateDefaultImplements()`.

Жизненный цикл имплементаций и состояние запуска (Этап 11, Путь 1):
- Локатор хранит ТИПЫ; экземпляры создаются В МОМЕНТ ЗАПУСКА — по экземпляру на
  исполнение (`PxContext.Implement(typeId)`, лениво). Состояние запуска допустимо
  держать в полях имплементации.
- Состояние запуска (`PxContext.State`, объект хоста — браузер, соединение, сервис…)
  попадает в имплементации конструктором (один параметр, совместимый с типом
  состояния) или через `PxContext.GetState<T>()`. В `PxRunOptions.State` его кладёт
  запускающий; `IPxRunManager.Start(request, state)` принимает его во владение и
  диспозит по завершении запуска (IDisposable/IAsyncDisposable; при Started=false —
  сразу). Хост, передавший state, сам его НЕ диспозит.
- Общие для разных типов блоков объекты — состояние запуска; данные, видимые
  пользователю, — переменные Blockly (PxValue).

### `src/Mars.PxBlocks/Mars.PxBlocks.Workspace` — RCL-редактор
- `JsSrc/` — TypeScript, сборка Vite в `wwwroot/dist/PxBlocks.js` (ESM, коммитится вместе
  с `wwwroot/media/`; загрузка через `import("./_content/Mars.PxBlocks.Workspace/dist/PxBlocks.js")`):
  - `index.ts` — `injectWorkspace` (прячет нативное меню категорий inline + resize),
    `updateToolbox`, `selectCategory`/`clearToolboxSelection`/`isFlyoutVisible` (flyout
    из Blazor-рейки), `setTypes`, `registerBlockDefinitions`,
    `saveWorkspace`/`loadWorkspace`/`clearWorkspace`/`undo`, `registerEvents`
    (синхронизирует выбор рейки событием `TOOLBOX_ITEM_SELECT`);
  - `renderer/` — порт рендерера «pxt» из PXT (`extends Blockly.zelos.*`, 9 файлов),
    `shapeFor()` дополнен чтением форм из реестра типов;
  - `connectionChecker.ts` — `PxConnectionChecker extends Blockly.ConnectionChecker`,
    зарегистрирован как `pxt`, включён опцией `plugins: { connectionChecker: 'pxt' }`;
  - `extensions/objectBuilder.ts` — mutator «создать объект»: кнопка «+» добавляет пары
    поле→значение, состояние через `saveExtraState/loadExtraState`.
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
- `PxSandboxEditor.razor` — **браузерная песочница** (до Этапа 9 — PxBlocksEditor):
  тулбар Undo/Redo/Clear/Center/Run/Stop + статус, панель вывода, автосейв в
  localStorage (Blazored.LocalStorage, ключ — `StorageKey`), исполнение in-process
  или на сервере (`RunTransport`, `RunMode`/`RunEventNames`). Единственный редактор,
  запускающий полный JSON программы из браузера.
- `PxToolboxRail.razor` — рейка категорий в стиле MakeCode: иконки (inline SVG), поиск
  (дебаунс 250 мс, временная flyout-категория «Поиск»), экспандер Advanced; выбранная
  категория заливается своим цветом; клик по выбранной закрывает flyout. Компактный
  режим (CSS, `data-rail` на корне редактора): Auto — container queries, при ширине
  ≤560px только иконки + поиск-кнопка со всплывающим полем; Compact — то же всегда.
- `wwwroot/pxblocks.css` — хром редактора: рейка, тёмный flyout, заголовки
  (`blocklyFlyoutHeading`), скрытие нативного меню категорий (`display:none !important` —
  `Toolbox.init()` ставит inline `display:block`). Подключается хостом link-ом в head
  (стенд — `App.razor`), НЕ инъекцией из скрипта.
- `PxWorkspaceJsInterop.cs` — ленивая загрузка ESM-модуля и вызовы JS.
- `e2e/check.mjs` — headless-проверки стенда системным Edge (playwright, `channel: 'msedge'`):
  замеры ширины svg/контейнеров + скриншоты (initial/category/resized).
- `e2e/check-browser.mjs` — проверка страницы `/browser`: загрузить пример, нажать Run,
  дождаться вывода (сценарий при этом реально открывает серверный Edge с Википедией).
- `e2e/check-flyouts.mjs` — flyout-ы стандартных категорий (Математика с мутаторами,
  Переменные со своим колбэком и созданием переменной через DOM-диалог Blockly 13).
- npm-инфраструктура: `package.json` (blockly 13.1.1), `vite.config.js` (lib → ESM),
  `tsconfig.json`, `copy-media.mjs` (media blockly → wwwroot/media).

### `src/Mars.PxBlocks/Mars.PxBlocks.Host.Shared` + `Mars.PxBlocks.Host` — серверное исполнение
- Host.Shared — контракты: DTO (`PxRunRequest` с клиентским RunId и `ContextName`,
  `PxRunResponse`, `PxRunResultDto`, `PxDefinitionsResponse`, `PxEditorContextInfo`),
  `IPxRunManager`, `IPxBlockCatalog`, `IPxBlocksBroadcaster`, `IPxBlocksApiClient`,
  `IPxRunTransport`, `IPxEditorContextRegistry`, `PxEditorContext` (fluent
  `PxEditorContext.Define("имя")…` — состав контекста и политика запуска), константы
  (маршрут `/_ws/pxblocks`, группа `pxblocks`).
- Host — `PxBlockCatalog` (определения + локатор имплементаций; toolbox = дефолт +
  доменные категории), `PxRunManager`+`PxRunSession` (разбор синхронно, исполнение
  фоном, события пакетируются 100 мс/256, политика из контекста — явные поля запроса
  в приоритете), `PxBlocksHub`, `PxBlocksController` (api/PxBlocks — ТОЛЬКО
  read-эндпоинты редактора: Definitions, Contexts, Contexts/{имя}; запуск из
  библиотеки убран в Этапе 10), `MainPxBlocks.AddPxBlocks/UsePxBlocks`
  (UsePxBlocks — ядерные PxEventBlocks; доменные сборки и контексты регистрирует хост).
- Определения блоков и реализации объявляются ТОЛЬКО на сервере; редактор получает
  их через api/PxBlocks (песочница и форма — Contexts/{имя}, Definitions — запасной
  «всё сразу»). Голого хранилища сценариев нет (решение 2026-08-16): JSON блоков
  хранят владельцы (ноды/админка) и передают в Run напрямую; запуск по месту — через
  IPxRunManager из DI.
- **REST запуска объявляет хост** (Этап 10): маршрут должен совпадать с ожиданием
  PxServerRunClient — `POST api/PxBlocks/Run` (тело PxRunRequest → PxRunResponse) и
  `POST api/PxBlocks/Stop/{runId:guid}` → bool. Образцы: PxRunController стенда и
  TestPxRunController сквозных тестов.

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
- **Браузерные скрипты** (контекст «browser», Этап 12): сценарии Playwright
  исполняются НА СЕРВЕРЕ в системном Edge (`channel: msedge`, видимое окно,
  SlowMo 50 — как Mars.E2E.Tests). Домен `Blocks/Browser/` в серверной сборке
  стенда: `PxBrowserBlocks` (demostand.playwright.goto/click/type/press/
  wait_selector/wait_ms/get_text/eval_js/print_texts), имплементации с инъекцией
  `PxBrowserRunState` конструктором (ленивый запуск браузера, `IAsyncDisposable` —
  диспозит PxRunManager), свой toolbox без Loop (событийные блоки контекста
  фильтруются `PxEditorContext.EventBlocks`), только событие Start. Селекторы/
  тексты — value-входы String c shadow-блоками из тулбокса (`PxToolboxBlock.InputsJson`).
  Состояние запуска создаёт PxRunController по `request.ContextName`.
- `tests/Test.Mars.PxBlocks` — xunit: сериализация toolbox, реестра типов, определений блоков.

## Сборка и запуск

```
# JS-бандл (после правок JsSrc):
cd src/Mars.PxBlocks/Mars.PxBlocks.Workspace
npm install          # один раз
npx tsc --noEmit     # проверка типов
npm run build        # Vite → wwwroot/dist + media

# Стенд:
dotnet run --project devstands/StandPxBlocksApp/StandPxBlocksApp/StandPxBlocksApp

# Тесты:
dotnet test tests/Test.Mars.PxBlocks
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

## Куда двигаться дальше

- Rich-редакторы полей: клик по полю → Blazor-форма (оверлей), удобно для объектов с множеством полей.
- Фишки PXT поштучно из `pxtblocks/*` (fields, flyout-hover, workspace search и т.п.).
- **Этап 7 — исполнение** (решение «вариант C» от 2026-08-14): сборка `Mars.PxBlocks.Runtime` —
  AST + tree-walking интерпретатор в .NET; control flow в ядре, листья — `IPxBlockImplement`
  по TypeId с регистрацией локатором (`RegisterAssembly`, паттерн `NodesLocator`); события
  исполнения с blockId — под подсветку бегущего блока и отладчик. Пошаговый план —
  в `Mars.PxBlocks.Workspace/PLAN.md`, Этап 7.
