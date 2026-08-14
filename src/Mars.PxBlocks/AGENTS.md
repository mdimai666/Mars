# Mars.PxBlocks — инструкция агенту

PxBlocks — визуальный редактор блоков в духе Microsoft PXT (MakeCode/Blockly) на Blazor.
Этапы 0–6 (редактор) готовы; без симулятора и без кодогенерации. Дальше — Этап 7:
исполнение блоков в .NET, решение «вариант C» от 2026-08-14: workspace JSON → AST →
tree-walking интерпретатор; control flow в ядре, листья — `IPxBlockImplement` по TypeId
(по образцу Mars.Nodes / INodeImplement), имплементации в отдельных сборках.
Детальный план — `Mars.PxBlocks.Workspace/PLAN.md`, Этап 7. Цели: `Mars.PxBlocks.Workspace/Mission.md`.

## Архитектура: гибридная обёртка Blockly

- Движок — официальный **blockly 13.1.1** (тот же пин, что у PXT) в браузере: рендеринг,
  drag&drop, стыковка, flyout, undo/redo, trashcan — всё Blockly.
- Blazor — оболочка: хром редактора, конфигурация (toolbox/типы/определения блоков), сериализация.
- Свой SVG-рендерер в Blazor не пишется (5 прошлых попыток провалились именно на этом).
- PXT целиком не подключается; его слой `pxtblocks/*` переносится пофайлово
  (референс: `C:\js\2026\microsoft-pxt`, при переносе заменять `pxt.*`-зависимости).

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
  `PxFieldText`/`PxMaster.Text`, `PxFieldDropdown`/`PxMaster.Dropdown`, `PxValueInput`/`PxMaster.Value`,
  `PxStatementInput`/`PxMaster.Do` (входы с `Check`).
- `PxBlocklyEvent` (пакет событий из JS), `PxWorkspaceState`.

### `src/Mars.PxBlocks/Mars.PxBlocks.Runtime` — исполнение (AST + интерпретатор)
Чистый .NET без JS (работает и в WASM): `Values/` — иерархия `PxValue` (Number/Boolean/
String/Object/List/Null); `Ast/` — узлы программы (каждый несёт blockId); `Parsing/` —
`PxParser` (Blockly JSON → AST; неизвестный лист → ошибка с blockId; форматы сверены
с blockly 13.1.1) + `PxCoreBlocks` (структурные type-id); `Execution/` — `PxInterpreter`
(control flow в ядре: if/циклы/break/процедуры/переменные/short-circuit; лимит шагов;
события BlockEntered/Exited/Output), `PxContext`, `IPxBlockImplement` +
`PxBlockImplementsLocator` (`RegisterAssembly`, паттерн NodesLocator); `Standard/` —
имплементации стандартных листьев (математика, логика, текст, `text_print`).
Точки входа: `PxParser.CreateDefault()`, `PxInterpreter.CreateDefaultImplements()`.

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
- `PxBlocksEditor.razor` — **редактор** поверх полотна: рейка + тулбар Undo/Redo/Clear,
  автосейв в localStorage (Blazored.LocalStorage, ключ — параметр `StorageKey`), дефолтная
  конфигурация из `Defaults/` (`PxDefaultToolbox`, `PxDefaultBlocks`), если потребитель
  не передал свою. Хост вставляет `<PxBlocksEditor />` + link на pxblocks.css в head.
- `PxToolboxRail.razor` — рейка категорий в стиле MakeCode: иконки (inline SVG), поиск
  (дебаунс 250 мс, временная flyout-категория «Поиск»), экспандер Advanced; выбранная
  категория заливается своим цветом; клик по выбранной закрывает flyout.
- `wwwroot/pxblocks.css` — хром редактора: рейка, тёмный flyout, заголовки
  (`blocklyFlyoutHeading`), скрытие нативного меню категорий (`display:none !important` —
  `Toolbox.init()` ставит inline `display:block`). Подключается хостом link-ом в head
  (стенд — `App.razor`), НЕ инъекцией из скрипта.
- `PxWorkspaceJsInterop.cs` — ленивая загрузка ESM-модуля и вызовы JS.
- `e2e/check.mjs` — headless-проверки стенда системным Edge (playwright, `channel: 'msedge'`):
  замеры ширины svg/контейнеров + скриншоты (initial/category/resized).
- npm-инфраструктура: `package.json` (blockly 13.1.1), `vite.config.js` (lib → ESM),
  `tsconfig.json`, `copy-media.mjs` (media blockly → wwwroot/media).

### Стенд и тесты
- `devstands/StandPxBlocksApp` — Blazor Web App + WASM для проверки редактора;
  `Home.razor` = `<PxBlocksEditor />` без аргументов. `ILocalStorageService` регистрируется
  и в серверном `Program.cs` (нужно для пререндера), и в клиентском.
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
