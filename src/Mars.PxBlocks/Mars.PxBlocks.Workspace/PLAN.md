# PxBlocks — план: аналог PXT (Blockly) на Blazor

Миссия: редактор блоков как в MakeCode/PXT. Сейчас — **только редактор**.
Без симулятора и без кодогенерации (выполнение блоков — декларативное, в .NET, позже).

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

### Будущее (вне текущего скоупа — «пока чисто редактор»)
- Декларативная модель по образцу Mars.Nodes: определения блоков наследованием
  классов; исполнение — отдельно (`IPxBlockImplement`, аналог `INodeImplement<TNode>`),
  регистрируется локатором в другой сборке (`NodesLocator` → `PxBlocksLocator`).
- Интерпретатор в .NET: workspace JSON → C#-AST → исполнение. Без кодогенерации.

## Структура проекта (по образцу Mars.Nodes.Workspace + EditorJsBlazored)

```
src/Mars.PxBlocks/
├─ Mars.PxBlocks.Shared/          # модели, сериализуемые, без JS
│  ├─ Toolbox/                    # PxToolboxCategory, элементы toolbox
│  ├─ Types/                      # PxType — реестр типов и правил стыковки
│  └─ Serialization/              # PxWorkspaceState ⇄ Blockly JSON
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

## Этапы (только редактор)

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
save/load через extraState). Дальше: rich-редакторы полей (Blazor-формы), PXT-фишки поштучно.

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
