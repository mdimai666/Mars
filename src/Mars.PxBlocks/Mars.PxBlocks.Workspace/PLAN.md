# PxBlocks — план: аналог PXT (Blockly) на Blazor

Миссия: редактор блоков как в MakeCode/PXT. Этапы 0–6 — редактор (готов).
Без симулятора и без кодогенерации; исполнение блоков — декларативное в .NET (Этап 7).

## Архитектура: гибрид — официальный Blockly + перенос слоя PXT

**Blockly не переписывать, PXT целиком не подключать.**

- Движок: npm **`blockly@13.1.1`** — тот же пин, что в `package.json` самого PXT.
- Внешний вид и поведение MakeCode: перенос нужных файлов из `pxtblocks/`
  (локальная копия: `C:\js\2026\microsoft-pxt`) пофайлово, заменяя `pxt.*`-зависимости.
- Blazor — оболочка: хром редактора, конфиг toolbox, определения блоков,
  сериализация, позже — декларативное исполнение.
- Мост: TypeScript, сборка Vite в `wwwroot/dist` (паттерн `EditorJsBlazored` из этого репо).

### Почему не PXT целиком
- PXT = целое приложение: **React 17** оболочка, TS-компилятор в браузере, симулятор,
  пакеты/облако/туториалы, сборка jake/gulp + свой CLI. Всё это нам не нужно.
- `pxtblocks/*` сцеплены с рантаймом PXT (`pxt.*`, `pxtlib.d.ts`, `lf()`) — целиком
  переносятся только вместе с потрохами PXT.
- Подключение «как есть» = iframe (чёрный ящик, свои блоки/типы не добавить)
  или форк с таргетом (поддержка гигантской сборки ради нескольких файлов). Тупик.

### Что именно берём из PXT (найденные места)
- **Рендерер «pxt»** — 5 файлов, подклассы Zelos:
  `pxtblocks/plugins/renderer/{renderer, constants, pathObject, info, drawer}.ts`
  (`extends Blockly.zelos.*`, `Blockly.blockRendering.register("pxt", Renderer)`).
- **Формы стыковок по типам** — `plugins/renderer/constants.ts`, `shapeFor()`:
  `Boolean` → HEXAGONAL (шестиугольник), `Number`/`String`/прочее → ROUNDED,
  statement-коннекторы → NOTCH. Это эталон для нашей системы типов.
- Позже, поштучно: `plugins/logic/ifElse.ts`, `plugins/functions/`, `fields/`,
  `composableMutations.ts`, `toolbox.ts`, `plugins/duplicateOnDrag/connectionChecker.ts`
  (пример кастомного `extends Blockly.ConnectionChecker`).

### Собственная система типов (поверх PXT-маппинга)
- Канонический реестр типов `PxType` живёт в **C#** (Mars.PxBlocks.Shared): имя, форма,
  правила совместимости/подтипов. В JS при старте уходит сериализованная матрица.
- JS-сторона: `check` на коннекторах + кастомный `ConnectionChecker` + расширение
  `shapeFor()` на наши типы (объекты и т.п.).
- Поведение «bool-шестиугольник не стыкуется с number» обеспечивается checker'ом,
  форма — визуальное следствие.

### Объекты с множеством полей (этап 5)
- Mutator-блок «создать объект» с раскладывающимися строками `поле → значение`
  (паттерн стандартных mutators Blockly).
- Rich field editor: клик по полю → форма-оверлей (Blazor-компонент).

### Исполнение блоков — решение «вариант C» (2026-08-14), план — Этап 7
- **Tree-walking интерпретатор в .NET.** workspace JSON → AST → `PxInterpreter`.
  Без кодогенерации и Roslyn: песочница «по построению» (бек исполняет пользовательские
  программы), лимиты шагов, ошибки/подсветка маппятся на blockId, исполнимо и в WASM.
- Отвергнуты: кодоген + Roslyn (песочница на сервере, нет в WASM, маппинг ошибок на
  блоки); «`BlockImplement` на каждый блок» (control flow размазывается по плагинам,
  теряются единые гарантии скоупов/break/short-circuit); Jint + JS-кодоген Blockly
  (чужая JS-семантика, конфликт с решением «декларативное исполнение в .NET»).
- **Control flow — в ядре интерпретатора, не плагинится**: последовательности, if/else,
  циклы, break/continue, процедуры, `variables_get/set`, short-circuit `logic_operation`.
- **Листья — `IPxBlockImplement` по TypeId** (аналог `INodeImplement<TNode>`),
  регистрация локатором `RegisterAssembly` (по образцу `NodesLocator`), имплементации
  в отдельных сборках.
- Все шаги `ValueTask` (задел на паузы/сенсоры); события исполнения с blockId
  (задел на подсветку исполняемого блока и отладчик).

## Структура проекта (по образцу Mars.Nodes.Workspace + EditorJsBlazored)

```
src/Mars.PxBlocks/
├─ Mars.PxBlocks.Shared/          # модели, сериализуемые, без JS
│  ├─ Toolbox/                    # PxToolboxCategory, элементы toolbox
│  ├─ Types/                      # PxType — реестр типов и правил стыковки
│  └─ Serialization/              # PxWorkspaceState ⇄ Blockly JSON
├─ Mars.PxBlocks.Runtime/         # исполнение: AST + интерпретатор (Этап 7)
└─ Mars.PxBlocks.Workspace/       # RCL-редактор
   ├─ JsSrc/                      # TypeScript-исходники (наши + портированные из pxtblocks)
   │  ├─ index.ts                 # export initWorkspace / api
   │  ├─ workspace.ts             # Blockly.inject + options
   │  ├─ renderer/                # порт pxtblocks/plugins/renderer/* (5 файлов)
   │  ├─ interop.ts               # события → .NET (DotNetObjectReference)
   │  └─ definitions.ts           # регистрация блоков из JSON-определений
   ├─ package.json / vite.config.js / tsconfig.json
   ├─ wwwroot/dist/               # артефакт Vite (ESM) + media/ из blockly
   ├─ PxBlocksEditor.razor(.cs)   # основной компонент
   ├─ Interop/PxWorkspaceJsInterop.cs
   └─ Components/                 # blazor-хром: тулбар, панели
devstands/StandPxBlocksApp/       # уже есть — стенд для проверки (WASM)
```

Загрузка JS: ESM через `import("./_content/Mars.PxBlocks.Workspace/dist/...")`
из JS interop — без правки App.razor стенда.

## Вес и сборка (факты из node_modules blockly 13.1.1)

| Что | Размер |
|---|---|
| `blockly.min.js` (core+блоки+en) | 776 КБ, gzip ~230–250 КБ |
| `blocks_compressed.js` (все стандартные блоки) | 496 КБ — берём только нужные |
| генераторы js/python/lua/php/dart | **не нужны** — в бандл не попадают |
| `media/` (иконки, звуки) | 19 КБ — копия в wwwroot |

- Сборка: `npm i` один раз + `npm run build` (Vite, ~1–2 с) при изменении TS.
  `dotnet build` не затрагивает. Прецедент: `EditorJsBlazored`.
- Загрузка ленивая (при открытии редактора) — страницы Mars не утяжеляет.
- Лицензия Blockly Apache-2.0 — совместима (атрибуция).

## Этапы

### Этап 0 — Фундамент ✅
`package.json` (blockly@13.1.1) + `vite.config.js` + `tsconfig`; `Blockly.inject`
в `PxBlocksEditor.razor` через interop; media в wwwroot.
✅ Стенд StandPxBlocksApp: пустой workspace, pan/zoom работают.

### Этап 1 — Рендерер «pxt» (вид MakeCode) ✅
Порт 5 файлов `pxtblocks/plugins/renderer/*` в `JsSrc/renderer/`,
регистрация рендерера, тема/цвета MakeCode.
✅ Блоки выглядят как в MakeCode: шестиугольники/скругления/пазлы.

### Этап 2 — Toolbox и базовые блоки ✅
Модель toolbox в Shared → toolbox JSON Blockly; категории как в MakeCode
(Основное/Циклы/Логика/Математика/Переменные/Текст и т.д.); стандартные блоки.
✅ Drag из flyout, стыковка next/value/statement, удаление, trashcan.

### Этап 3 — Полный редактор ✅
Save/load (`Blockly.serialization` + localStorage), undo/redo, zoom-контролы,
контекстное меню, события JS→.NET пакетированием.
✅ Перезагрузка страницы восстанавливает схему.

### Этап 4 — Система типов ✅
Реестр `PxType` в C# → матрица совместимости в JS; кастомный `ConnectionChecker`;
`shapeFor()` на наших типах.
✅ Несовместимые типы физически не стыкуются; формы как в PXT.

### Этап 5 — Определения блоков и объекты ✅
Определения блоков — классы C# наследованием (`PxBlockDefinition` → Blockly JSON);
блок «создать объект» с динамическими парами поле→значение (расширение Blockly,
save/load через extraState).

### Этап 5.1 — Эргономика объявлений ✅
fluent-API `PxMaster.Define("id")…` (аналог аннотаций на функциях в PXT) + группировка
`PxBlockSet` по областям; именованные плейсхолдеры `{имя}` в сообщениях — порядок
аргументов выводится из строки. Классы-наследники остались только для блоков
с мутаторами/динамикой. Дальше: rich-редакторы полей (Blazor-формы), PXT-фишки поштучно.

### Этап 6 — Тулбокс в стиле MakeCode ✅
Эталон: скриншот micro:bit MakeCode. Белая рейка категорий с иконками и поиском,
выбранная заливается цветом категории; тёмный flyout с заголовками и цветными блоками.
Происхождение слоёв (проверено по microsoft-pxt):
- рейка (иконки/поиск/«more»/Advanced) — React-шелл MakeCode, `webapp/src/toolbox.tsx` —
  в pxtblocks и Blockly этого нет; аналог пишем в Blazor (внутри PxBlocksEditor);
- flyout — Blockly: контент-заголовки собирает pxtblocks (`pxtblocks/toolbox.ts`,
  `builtins/*`, `hideFlyoutHeadings`), вид — CSS шелла `theme/blockly-core.less`
  (`--pxt-neutral-background3` и т.п.). Переносится: CSS + `kind: label` в toolbox JSON.

Шаги:
1. Стилизация flyout: перенести CSS из `theme/blockly-core.less` (фон, заголовки,
   label, кнопки); `PxToolboxLabel` (kind=label) в модели + заголовки в дефолтных
   категориях.
2. Рейка `PxToolboxRail` в PxBlocksEditor: категории из PxToolbox (Icon/Colour/Advanced),
   поиск, «more»; нативное меню категорий Blockly скрыть.
3. Interop `selectCategory(name)` → `workspace.getToolbox().selectCategoryByName(...)`;
   поиск — временная flyout-категория из подходящих блоков.
4. Иконки категорий — inline SVG в рейке (набор свой или из `svgicons/` PXT).

Реализовано: `PxToolboxRail.razor` (иконки inline SVG, поиск с дебаунсом 250 мс,
экспандер Advanced; «more» как в MakeCode не воспроизводился), `PxToolboxLabel`
(kind=label + web-class), тёмный flyout и стили рейки в `wwwroot/pxblocks.css`
(подключается из `index.ts`), interop `selectCategory`/`clearToolboxSelection`,
нативное меню категорий скрыто CSS. Поиск — временная категория «Поиск»
(совпадение по имени категории или типу блока). Клик по выбранной категории
закрывает flyout. Категории: `Blocks` → `Items` (блоки + метки), `Icon`/`Advanced`.

### Этап 7 — Исполнение блоков в .NET (AST + интерпретатор) ✅
Решение «вариант C» (2026-08-14): tree-walking интерпретатор; control flow в ядре;
листья — плагинные имплементации. Без кодогенерации и симулятора.

Новая сборка `src/Mars.PxBlocks/Mars.PxBlocks.Runtime` — чистый .NET без JS:
исполним и in-process (стенд/WASM), и на серверном беке.

```
Mars.PxBlocks.Runtime/
├─ Values/       # PxValue: Number/Boolean/String/Object/List — зеркало PxTypeRegistry
├─ Ast/          # PxProgram, PxStatement*, PxExpression*; у каждого узла BlockId
├─ Parsing/      # PxParser: PxWorkspaceState.BlocksJson → AST
├─ Execution/    # PxInterpreter (ядро), PxContext (скоупы, вывод, события, лимиты),
│                # IPxBlockImplement, локатор имплементаций
└─ Standard/     # имплементации стандартных блоков-листьев
```

Шаги:
1. **Каркас Runtime.** `PxValue` (+приведения по правилам `PxTypeRegistry`);
   `PxContext` — скоупы переменных, поток вывода, события, `CancellationToken`,
   лимит шагов (защита от бесконечных циклов).
2. **AST + `PxParser`.** Blockly JSON → `PxProgram`; каждый узел несёт `id` блока;
   неизвестный тип блока/входа → ошибка парсинга с blockId. Верхний уровень — стеки
   statement-блоков (позже: блоки-события «when …» как в MakeCode).
3. **Ядро `PxInterpreter`** (control flow, не плагинится): последовательное исполнение;
   `controls_if` (включая else-if/else); `controls_repeat_ext`, `controls_whileUntil`,
   `controls_for`, `controls_forEach`; break/continue (`controls_flow_statements`);
   процедуры — `procedures_defnoreturn/defreturn`, `procedures_callnoreturn/callreturn`,
   `procedures_ifreturn`: аргументы, локальные переменные, рекурсия; `variables_get/set`;
   short-circuit `logic_operation`. Ошибки исполнения — с blockId.
4. **`IPxBlockImplement` + локатор.** `Evaluate(ctx, args) → ValueTask<PxValue>`
   для output-блоков, `ExecuteAsync(ctx, args)` для statement-блоков; аргументы
   приходят уже вычисленными (кроме короткого замыкания — оно в ядре). Регистрация
   `RegisterAssembly` (паттерн `NodesLocator`). Стандартные листья: литералы
   `math_number`/`text`/`logic_boolean`, `math_arithmetic`, `math_number_property`,
   `logic_compare`, `logic_negate`, `text_join`, `text_length`, `text_print`.
5. **Тесты** (Test.Mars.PxBlocks): фикстуры Blockly JSON → AST; семантика control flow
   (вложенные if, границы циклов, break из вложенного, скоупы функций, рекурсия,
   short-circuit); лимит шагов; неизвестный блок; ошибки с blockId.
6. **Стенд.** Кнопка «Run» в тулбаре `PxBlocksEditor` — исполнение in-process; панель
   вывода (`text_print`); подсветка исполняемого блока — interop `highlightBlock(id)`
   (CSS-класс на SVG-группе блока) по событиям ядра.

Дальше (вне этапов): доменные пакеты блоков со своими `IPxBlockImplement` в отдельных
сборках; серверный бек — тот же Runtime + стриминг событий (SignalR); паузы («ждать
N мс», сенсоры) на async-каркасе.

Реализовано: `Mars.PxBlocks.Runtime` (Values/Ast/Parsing/Execution/Standard; чистый
.NET — собрался и в стенде-WASM): `PxValue`-иерархия (Number/Boolean/String/Object/
List/Null), `PxParser` (форматы сверены с blockly 13.1.1: extraState `controls_if`/
`procedures_*`/`text_join`, поля-переменные `{"id"}`, тени как дефолты сокетов,
`disabledReasons`), ядро `PxInterpreter` (if/else-if/else, repeat/while/for/forEach,
break/continue, процедуры с параметрами и рекурсией, скоупы, short-circuit, лимит
шагов, события BlockEntered/Exited/Output с blockId), локатор `IPxBlockImplement`
+ стандартные листья (литералы, арифметика/тригонометрия/свойства чисел, сравнение,
текстовые блоки, `text_print`), тесты 49 шт. (парсер/семантика/лимиты/short-circuit/
события). Стенд: кнопки Run/Stop, панель вывода, подсветка бегущего блока
(interop `setBlockHighlight`), ошибка подсвечивает виновный блок; демо-имплементации
`PxDemoBlockImplements` в Workspace. Ограничение v1: `lists_*` не поддерживаются
(неизвестный блок → ошибка с id), блоки «when …»-событий — позже.

Дополнение (2026-08-15): событийные блоки **Start/Loop — аналог Arduino setup()/loop()**.
`px_start`/`px_loop` — хат-блоки без prev/next (`PxBlockDefinition.Hat` → `style.hat`
в Blockly JSON, шапку рисует zelos-ядро), парсер сводит их в `PxEventBlock`
(`PxEvents.Start`/`PxEvents.Loop`). Семантика: обычные стеки и события Start идут
в порядке workspace, события Loop — после всех и повторяются (выход — break или Stop).
Режимы запуска `PxBlocksEditor`: `RunMode=AllTopLevel` (по умолчанию — как раньше)
и `RunMode=Events` + `RunEventNames` (массив имён; в рантайме — `PxRunOptions.EventNames`).
В режиме Events запуск идёт **фазами в порядке списка**: сначала ВСЕ события с первым
именем (в порядке workspace), затем со вторым и т.д. — при `["start","loop"]` Loop
гарантированно после Start независимо от раскладки на полотне. В режиме по умолчанию
Loop тоже всегда после всех (включая Start). В редакторе лимит шагов снят (`StepLimit=0` —
бесконечный loop живёт до Stop), вывод ограничен 1000 строк. Тулбокс: категория
«Основное» (иконка-флаг в рейке). Тесты — 58 шт.

Фикс шапки (2026-08-15): `jsonInit` Blockly читает `style.hat` один раз и обнуляет
`style` прямо в общем JSON определения — шапка оставалась только у первого созданного
экземпляра блока (flyout → перетаскивание → flyout теряли шапку). Теперь шапка ставится
расширением `px_hat_cap` (`JsSrc/extensions/hat.ts`, вызывается при каждом создании
блока); `PxBlockDefinition.Hat` генерирует `extensions: ["px_hat_cap"]` вместо `style`.
Тестов — 59.

## Что делаем со старым кодом
- **Удаляем** (свой рендеринг, заменён Blockly): PxWorkspace.razor, PxBlockComponent.razor,
  PxBlockSvgHelper.cs, PxToolbox.razor, pxWorkspaceJs.js, PxBlock.cs, PxField.cs, PxInput.cs.
  Всё остаётся в git-истории.
- **Переиспользуем**: PxBlockDefinition, PxToolboxCategory, PxWorkspaceState как
  конфиг-модели; набор категорий из старого PxBlocksEditor.razor — контент Этапа 2;
  опыт field-editor-оверлея пригодится в Этапе 5.

## Риски
- Портируемые файлы pxtblocks написаны под blockly своей эпохи — пинним ту же
  версию 13.1.1, что у PXT; точечные `pxt.*`-зависимости заменяем при переносе.
- Точки сцепления с внутренностями Blockly: checker, shapeFor, свои поля/мутаторы —
  держим в отдельных модулях JsSrc, чтобы переживать апгрейды Blockly.
- Поток событий на больших схемах — пакетирование закладываем сразу (Этап 3).
- Контракт парсера (Этап 7) — Blockly JSON версии blockly 13.1.1, включая extraState
  мутаторов: фиксируем фикстурами в тестах, чтобы переживать апгрейды Blockly.
