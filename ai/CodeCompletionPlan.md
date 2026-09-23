# CodeCompletionPlan — Mars.CodeCompletion

Отдельный модуль серверного IntelliSense (Roslyn) для Monaco-редакторов Mars.
Первый потребитель — FunctionNode (CSharpScript, top-level statements, глобальный объект:
`return msg.Payload;`). Модуль **не зависит от нод** — ноды зависят от него.

## Решения (подтверждены пользователем 2026-09-24)

- **Архитектура A**: HTTP-endpoints + C#-провайдеры BlazorMonaco. Без LSP в первой итерации;
  серверный core проектируем транспорта-агностично (LSP — план Б).
- **Полный набор**: completion + hover + signature help + диагностика (маркеры).
- **Имя**: `Mars.CodeCompletion`; флаг `FeatureFlags.CodeCompletion`.
- **Пока внутри решения** (Mars.slnx), NuGet-пакетизируем позже, когда API устоится.

## Отклонённые альтернативы (исследование 2026-09-24)

- `roslyn-language-server` (NuGet, 5.9.x-preview): out-of-process exe (stdio/pipe),
  **не поддерживает CSharpScript-режим** (top-level return + global object), сотни МБ RAM.
- WASM-Roslyn в браузере: roslyn#70404 (`CompletionService.GetService` → null в WASM),
  roslyn#84615 (падение на .NET 11 preview), 150–250 МБ на клиента.
- npm `monaco-languageclient`: требует подмену monaco-editor на `@codingame/monaco-vscode-api`
  — несовместимо с BlazorMonaco.
- Старая альфа (дек 2025, удалена в `89eeb8a9`; код — в `ba4139a8`): рабочая схема, но
  новый Document на каждый запрос, фейковый global-тип, пути к сборкам **с клиента**.
  Забираем из неё: mapping Roslyn tags → Monaco CompletionItemKind (`MySemantic.cs`),
  состав эндпоинтов, общую форму DTO.
- На будущее: в бандле monaco-editor внутри BlazorMonaco 3.5.0 **уже есть** `monaco.lsp`
  (`MonacoLspClient`, `WebSocketTransport`) — фундамент для LSP-транспорта без npm-пакетов.

## Состав

> Уточнение 2026-09-24 (реализация): три проекта вместо двух — DTO вынесены в
> `Mars.CodeCompletion.Contracts` (чистые POCO), т.к. серверный проект тянет Roslyn/ASP.NET
> и не может ссылаться из WASM. Серверный проект — `Mars.CodeCompletion.Host` (конвенция `.Host`).
> `ICodeContextProvider` живёт в Host (Roslyn-типы в сигнатуре). Клиент API — через
> `IMarsWebApiClient` (гайд §5): `ICodeCompletionServiceClient` в `Mars.WebApiClient`.
> BlazorMonaco 3.5 API (проверено по dll/исходникам): `BlazorMonaco.Languages.Global.RegisterCompletionItemProvider(jsRuntime, LanguageSelector, CompletionItemProvider{TriggerCharacters, ProvideMethod})`,
> `RegisterHoverProviderAsync`, `BlazorMonaco.Editor.Global.SetModelMarkers(jsRuntime, model, owner, markers)`;
> провайдеры регистрируются **глобально на язык** — в C# нужен реестр modelUri → contextId.
> Событие контента — `StandaloneCodeEditor.OnDidChangeModelContent`.

### 1. Сервер — `src/Mars.Modules/Mars.CodeCompletion.Host/` (Microsoft.NET.Sdk)

Контракты — `src/Mars.Modules/Mars.CodeCompletion.Contracts/` (чистые POCO, без Roslyn):

- DTO: `CompletionRequest { DocumentId, Code, Offset }` → `CompletionItemDto { Label, Kind, InsertText, Detail, Documentation }`;
  `HoverRequest/Result`, `SignatureHelpRequest/Result`, `DiagnosticsRequest/Result` (offsetFrom/offsetTo/severity).

Host:

- `ICodeContextProvider` — «каждый провайдер объявляет свой контекст кода»:
  - `ContextId` (slug, используется в URL), `HostObjectType` (global object скрипта),
  - `Imports`, `MetadataReferences` (лениво), `IsScript` (ParseOptions `SourceCodeKind.Script`),
  - опционально сниппеты.

- `CodeCompletionWorkspaceManager` (singleton): `MefHostServices` лениво, один на процесс
  (сборки `Microsoft.CodeAnalysis.Features` / `CSharp.Features` — пины в Directory.Packages.props).
  Per `contextId` — `AdhocWorkspace` + `Project` (`ProjectInfo.Create(..., hostObjectType: ...)`,
  imports, references, script parse options). Per `DocumentId` (Guid клиента) — `Document`,
  обновление полным текстом `SourceText.From(code)` (инкрементальные `WithChanges` — фаза 2).
- Сервисы поверх Features-слоя: `CompletionService` (+`GetDescriptionAsync`), `QuickInfoService`,
  `SignatureHelpService`, диагностика — `compilation.GetDiagnostics()` → offsets.
- `MainCodeCompletion`: `AddMarsCodeCompletion()` / `UseMarsCodeCompletion()` (по `ai/FeatureIntegrationGuide.md`).
- `CodeCompletionController`: `[FeatureGate(FeatureFlags.CodeCompletion)]`, `[Authorize(Roles = "Admin")]`:
  - `GET api/CodeCompletion/info` → `{ enabled, contexts[] }` (клиент так узнаёт о включённом флаге;
    при выключенном `[FeatureGate]` даёт 404 → провайдеры не регистрируются);
  - `POST api/CodeCompletion/{contextId}/completion|hover|signature|diagnostics`.

### 2. Клиент — `src/Mars.Modules/Mars.CodeCompletion.Front/` (RCL)

- `ICodeCompletionClient` — Flurl через существующий механизм админки (`IMarsWebApiClient`,
  гайд §5; сверить с текущим состоянием при реализации).
- `CodeCompletionAttacher`: для данного `StandaloneCodeEditor` + `contextId`:
  - completion/hover — штатные `RegisterCompletionItemProvider` / `RegisterHoverProviderAsync` BlazorMonaco 3.5
    (callback'и в C#, JS не нужен);
  - диагностика — `SetModelMarkers` с debounce (~500 мс) после изменения текста;
  - **signature help**: в BlazorMonaco нет Register-моста (проверено в dll/jsInterop 3.5.0) →
    свой маленький JS `wwwroot/codeCompletionInterop.js`
    (`monaco.languages.registerSignatureHelpProvider` + DotNetObjectReference-bridge),
    динамический `import` из компонента; cache-busting `?v=` + bump `MarsAppVersion` при коммите;
  - старт: один `GET info` (кэш в scoped-сервисе) → флаг выключен/404 → ничего не регистрируем.

### 3. Интеграция FunctionNode (на стороне нод)

- `FunctionNodeContextProvider : ICodeContextProvider` — регистрация в DI на хосте нод:
  `ContextId = "nodes.function"`, `HostObjectType = FunctionNodeImpl.ScriptExecuteContext`,
  imports/references **зеркалят** `FunctionNodeImpl.ScriptOptions` + сборки из `IServiceCollection`
  + regex-детект `RNS.GetService<...>` по тексту (та же логика, что в рантайме — серверная сторона,
  никаких путей с клиента).
- `CodeEditor2` (`src/Modules/MarsCodeEditor2`) — новый опциональный параметр
  `CompletionContextId` (string?): если задан, через `CodeCompletionAttacher` вешает провайдеры.
  Reference: MarsCodeEditor2 → Mars.CodeCompletion.Front.
- `FunctionNodeForm.razor` / `InlineFunctionNodeForm.razor`: передать `CompletionContextId="nodes.function"`.

### 4. Флаг и подключение

- `FeatureFlags.CodeCompletion` в `src/Server/Mars.Server.Abstractions/Features/FeatureFlags.cs`.
- `Mars.WebApp/appsettings.json` → `FeatureManagement.CodeCompletion: false`; Development — `true`.
- `MarsWebAppStartup`: `builder.AddIfFeatureEnabled(...)` + `app.UseIfFeatureEnabled(...)`.
- RAM: MEF-хост и workspace'ы поднимаются **лениво** (первый запрос), при выключенном флаге — никогда.

### 5. Раствор и пакеты

- Пины в `Directory.Packages.props`: `Microsoft.CodeAnalysis.Features`,
  `Microsoft.CodeAnalysis.CSharp.Features` (5.9.0, рядом с существующими Roslyn-пинами).
- `Mars.slnx`: виртуальная папка `/Mars.Modules/CodeCompletion/` + два проекта.

### 6. Тесты — `tests/Mars.CodeCompletion.Tests` (xUnit v3 MTP)

- Скрипт-контекст: top-level `return` валиден; `msg.` даёт члены global-объекта; локальные переменные видны.
- Completion: kind-маппинг, presence `RNS`/`Send`.
- Диагностика: ошибка компиляции → offset/severity.
- Hover и signature help на `Send(`.
- Фейковый `ICodeContextProvider` (без нод) — модуль тестируется изолированно.

## Проверка

```
dotnet build Mars.slnx
dotnet build tests/Mars.CodeCompletion.Tests && tests\Mars.CodeCompletion.Tests\bin\Debug\net10.0\Mars.CodeCompletion.Tests.exe
```

Ручная: WebApp с `"CodeCompletion": true` → форма FunctionNode → Ctrl+Space, `msg.` → hover →
`Send(` → signature → красный маркер на ошибке. С флагом `false` — провайдеры не регистрируются,
эндпоинты 404, Roslyn-сборки не грузятся.

## Порядок работ (статус)

Ветка: `ai/node-rework-stage3-CodeCompletion`. План НЕ закрывать — после проверки пользователя
будут доработки.

1. [x] Пины пакетов (Features/CSharp.Features 5.9.0), slnx, скелеты 3 проектов — собираются.
2. [x] Серверный core: DTO (Contracts), `ICodeContextProvider`, `CodeCompletionWorkspaceManager`
   (ленивый MEF, персистентный workspace на контекст, документ на client DocumentId,
   обновление текста через `TryApplyChanges(WithDocumentText)`), `CompletionQueryService`,
   `HoverQueryService` (QuickInfoService, первая секция — code fence), `DiagnosticsQueryService`,
   `SignatureHelpQueryService` — вручную по SemanticModel (порт альфы: `SignatureHelpService`
   в Roslyn 5.9 internal), маппинг Roslyn tags → Monaco kinds.
3. [x] `CodeCompletionController` (`api/CodeCompletion/info` + `{contextId}/completion|hover|signature|diagnostics`,
   `[FeatureGate]` + `[Authorize(Roles="Admin")]`), `MainCodeCompletion.AddMarsCodeCompletion()`,
   флаг `FeatureFlags.CodeCompletion`, appsettings (prod false / dev true),
   ProjectReference + `AddIfFeatureEnabled` в `MarsWebAppStartup`.
4. [x] Тесты серверной части — `tests/Mars.CodeCompletion.Tests` (8 фактов: script top-level return,
   globals в completion, импорты, диагностика с offsets/CS0103, hover, signature help + активный
   параметр, NotFoundException на неизвестный контекст). Все зелёные.
   **Ключевая находка:** Roslyn применяет `ProjectInfo.HostObjectType` (globals) ТОЛЬКО при
   `isSubmission: true` (`RegularCompilationTracker`: `IsSubmission ? CreateSubmissionCompilation(..., HostObjectType) : CreateCompilation(...)`);
   плюс документ обязан иметь `DocumentInfo.Create(sourceCodeKind: Script)` — иначе document-level
   Regular перекрывает parse options проекта (CS8805, globals не видны).
5. [x] Front: `ICodeCompletionServiceClient`/`CodeCompletionServiceClient` + `CodeCompletion`
   в `IMarsWebApiClient`; `Mars.CodeCompletion.Front`: `CodeCompletionRegistry` (singleton,
   `ICodeCompletionAttacher`) — ленивая инициализация (GET info → флаг), глобальная регистрация
   completion/hover провайдеров BlazorMonaco на `csharp`, реестр modelUri → (contextId, documentId),
   hover-range считается из offsets на клиенте; JS-модуль `codeCompletion.js` (динамический import,
   `?v=` cache-busting): signature help провайдер + snapshot (code/offset) + debounce-диагностика
   (500 мс) + маркеры + очистка по onWillDispose. Kind в DTO — **строка** (имя Monaco-вида),
   т.к. нумерация enum BlazorMonaco (Method=0…) отличается от LSP — парсится `Enum.TryParse`.
   Graбли: `CompletionItemProvider` без параметренного ctor — `new CompletionItemProvider(null, ProvideDelegate)`;
   `InsertText`/`Detail`/`SortText`/`FilterText` — plain string, `Label`/`Documentation` — JsonElement (AsString-хелперы).
6. [x] Интеграция FunctionNode: `FunctionNodeContextProvider` в `Mars.Nodes.Host/Services`
   (зеркалит ScriptOptions `FunctionNodeImpl`: `ScriptExecuteContext` как HostObjectType, те же
   импорты, definedAssemblies + все сборки сервисов из DI; TPA-базу добавляет менеджер),
   регистрация в `MainNodes.AddMarsNodes`; константа `NodeCompletionContexts.FunctionNode = "nodes.function"`
   в `Mars.Nodes.Contracts`; `FunctionNodeForm` — `OnInit="OnEditorInit"` → `ICodeCompletionAttacher.AttachAsync`
   (опциональный сервис через `IServiceProvider.GetService`, форма реализует `IAsyncDisposable`);
   `AddCodeCompletionFront()` в `Mars.Admin/Program.cs`. `dotnet build Mars.slnx` — зелёный, 8/8 тестов.
7. [ ] Ручная проверка, bump `MarsAppVersion` (новый JS-ассет).

## Грабли

- **Script-контекст (НАЙДЕНО ЭМПИРИЧЕСКИ, тесты):** `isSubmission: true` + `hostObjectType`
  в `ProjectInfo.Create` и `sourceCodeKind: Script` в `DocumentInfo.Create` — оба обязательны,
  иначе globals (`msg`) не видны и/или top-level `return` даёт CS8805.
- MEF: для `CompletionService`/QuickInfo/SignatureHelp нужны именно Features-сборки в
  `MefHostServices.Create` (у нас — `Assembly.Load` четырёх сборок, гарантированных
  PackageReference; публичных типов-якорей у Features нет).
- Completion-запросы идут из WASM (.NET-callback BlazorMonaco) — не спамить сервером:
  запрос только при открытии suggest-виджета/триггер-символе, диагностика — debounce.
- `info` — под `[Authorize]`: список контекстов наружу не отдавать анонимам.
- JS-ассет фронта: `?v=` + bump версии (конвенция cache-busting).

## Открытые вопросы (решаем по ходу)

- Точное место регистрации `FunctionNodeContextProvider` (startup нод-хоста) — выбрать при реализации.
- Нужен ли сниппет-провайдер в первой итерации (старый UI `CodeEditorSuggestSearchInput` с
  `di:services`-словарями остаётся как есть — не трогаем).
