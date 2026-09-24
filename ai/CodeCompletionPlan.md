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
7. [x] `MarsAppVersion` 0.8.3-alpha.20 → **0.8.3-alpha.21** (новый JS-ассет `codeCompletion.js`).
   Сборка `Mars.slnx` зелёная; `Mars.Nodes.Tests` 566/566; `Mars.CodeCompletion.Tests` 8/8.
   В тестовых хостах FeatureManagement не задан → CodeCompletion выключен, интеграционные тесты не затронуты.
8. [ ] **Ручная проверка пользователем** (браузер — только по его команде):
   dev-запуск WebApp (в `appsettings.Development.json` флаг уже `true`) → админка → форма
   FunctionNode: Ctrl+Space / `msg.` → подсказки; hover; `Send(` → signature help; ошибка в коде →
   красный маркер через ~0.5 c. С флагом `false`: `GET api/CodeCompletion/info` → 404,
   провайдеры не регистрируются, Roslyn MEF не поднимается.

## Доработки по первой проверке (2026-09-25)

- Симптом: форма открыта, подсказок нет, сетевых запросов нет. Причины тихого провала в цепочке:
  attacher не зарегистрирован в хосте → `GetService` = null (молча); `IsEnabledAsync` глотал
  любое исключение и **кэшировал false навсегда**.
- [x] `devstands/StandNodesApp` не был подключён вообще (ни client, ни server) — добавлено:
  `AddCodeCompletionFront()` (Client), `AddFeatureManagement` + `AddMarsCodeCompletion()` (Server),
  `FeatureManagement.CodeCompletion: true` в appsettings стенда.
- [x] Консольная диагностика цепочки (префикс `[CodeCompletion]`): attacher null / info ответ /
  init failed / model not available / attached. Ошибка info-проверки больше не кэшируется
  (`_enabledTask = null` → ретрай на следующем attach).
- [x] **КОРЕНЬ БАГА «0 подсказок» (найден на стенде curl-зондами, воспроизведён тестами):**
  Roslyn submission-проект с НЕСКОЛЬКИМИ документами не поддерживается — completion работает
  только для первого документа, остальные молча возвращают пустой список (цепочка submissions
  строится через project references, не через соседние документы; стек с сервера:
  `CSharpCompilation.IsSubmissionSyntaxTree` → `Enumerable.SingleOrDefault` →
  «Sequence contains more than one element» при втором дереве в submission-компиляции).
  В альфе-декабре был один
  документ на запрос, поэтому она «работала». Фикс: **проект на каждый клиентский DocumentId**
  (один документ в проекте), текст обновляется на месте; `RemoveDocument` (DELETE-эндпоинт +
  вызов с фронта при detach/dispose модели) удаляет проект. Регресс-тесты:
  `CompletionPoisonBisection` (A–E), `Completion_still_works_after_diagnostics` — 18/18 зелёные.
- [x] Логирование тихих веток: `GetCompletionsAsync == null` (с состоянием compilation),
  `compilation == null`, исключения сервисов (LogError) — раньше проглатывались.
- Проверено curl на стенде (5288): `Console.` → 50, `RNS.` → 24, hover `(field) string NodeId`,
  signature `void ScriptExecuteContext.Send(object msgOrPayload, int output = 0)`,
  CS0103 sev8 на мусорном коде, чистый код → 0 диагностик.
- [ ] **ОТКРЫТЫЙ ВОПРОС — `msg.` → 0 подсказок:** `ScriptExecuteContext.msg` — `dynamic`
  (рантайм оборачивает NodeMsg в DynamicNodeMsgWrapper), Roslyn не дополняет dynamic.
  Варианты: (a) синтетический globals-тип для completion с `NodeMsg msg` (риск расхождения
  с рантаймом, но `msg.Payload` — центральный сценарий); (b) оставить как есть. Ждём решения.

### Статус на паузе (2026-09-25) — продолжать отсюда

**Блокер №1 — ЗАКРЫТ (2026-09-25): `net_http_operation_started` в браузере, attach не выполнялся.**
Симптом (консоль стенда у пользователя):

```
[CodeCompletion] info check failed: net_http_operation_started
[CodeCompletion] attach skipped for 'nodes.function': feature disabled on server
```

Сервер при этом живой: `curl /api/CodeCompletion/info` → `{"enabled":true,"contexts":["nodes.function"]}`.

**Корень (подтверждён по исходникам runtime и Flurl):** к JS-interop кадру отношения НЕ имеет
(гипотеза паузы про interop-continuation — неверна, `Task.Yield` не нужен).
`net_http_operation_started` бросает `HttpClient.CheckDisposedOrStarted()` — сеттеры
`BaseAddress/Timeout/DefaultRequestVersion/...` запрещены после первого отправленного запроса.
Конструктор `FlurlClient(HttpClient)` **всегда** делает `httpClient.Timeout = Timeout.InfiniteTimeSpan`
(Flurl src/Flurl.Http/FlurlClient.cs:83). Оба WASM-хоста регистрировали
`AddScoped<IFlurlClient>(sp => new FlurlClient(httpClient))` на ОДИН общий `HttpClient`,
созданный в Program.cs. В WASM скоуп один на приложение, поэтому штатный `IFlurlClient`
создаётся рано (до первого запроса) и всё работает; а `CodeCompletionRegistry` (singleton)
создаёт **собственный скоуп** → второй `new FlurlClient(общий httpClient)` уже после первых
запросов приложения → сеттер Timeout → исключение → `IsEnabledAsync` = false → attach skipped.
(Сообщение — сырой ключ ресурса из-за инвариантной глобализации/тримминга WASM.)

**Фикс:** `IFlurlClient` создаётся eagerly ОДИН раз в Program.cs и регистрируется готовым
инстансом (`AddScoped(sp => flurlClient)`) — `Mars.Admin/Program.cs` и
`StandNodesApp.Client/Program.cs`. Повторных конструкторов `FlurlClient` поверх общего
`HttpClient` больше нет; любой будущий singleton-со-скоупом потребитель тоже защищён.

**Блокер №2 — ЗАКРЫТ (2026-09-25): swagger/глобальный completion отдавал 13 910 элементов / 3 МБ и вешал вкладку.**
Фикс по стандартной схеме удалённых completion-серверов (clangd `--limit-results`, дефолт 100;
LSP `CompletionList.isIncomplete`): кап **200** элементов в `CompletionQueryService`
(`MaxCompletionItems`) + `Incomplete=true` в `CompletionResponseDto`, фронт пробрасывает флаг в
Monaco (`CompletionList.Incomplete`) → при продолжении ввода Monaco перезапрашивает сервер
вместо локальной фильтрации. ВАЖНО (эмпирика, тесты): Roslyn `ItemsList` **не отсортирован и
не отфильтрован по префиксу** (SortText = просто имя, приоритетных бакетов нет — алфавитный
порядок как в VS Code C# Dev Kit), поэтому сервер сам: фильтр `FilterText.StartsWith(префикс
слева от курсора, OrdinalIgnoreCase)` → сортировка SortText/FilterText → кап. Пустая строка =
топ-200 по алфавиту + incomplete; `Send`/`localVar` появляются при наборе префикса (дозапрос).
Прогрев на attach пользователь отклонил («подождать первый — гуд»). Тесты:
`Empty_prefix_completion_is_capped_and_incomplete`, `Prefixed_completion_is_filtered_and_complete`,
`Script_completion_includes_globals_methods_and_locals` переписан на префиксы — 20/20 зелёные.

**Окружение (обновлено 2026-09-25, вторая сессия):**
- Ветка `ai/node-rework-stage3-CodeCompletion`; коммиты: `90778c2e`, `b21ab9df`,
  `4e4534bc` (фикс блокера №1 + alpha.22 + emmet-guard; пользователь проверил — работает).
  Рабочее дерево чистое.
- Стенд StandNodesApp (background task отменён по завершении проверки); перезапуск:
  `dotnet run --project devstands\StandNodesApp\StandNodesApp --urls http://localhost:5288`.
- WebApp пользователя на 5003 поднимался ДО фикса project-per-document — нужен перезапуск.
- Тесты: `Mars.CodeCompletion.Tests` 18/18, `Mars.Nodes.Tests` 566/566, `dotnet build Mars.slnx` зелёный.

## Auto-using для неимпортированных типов + урезка импортов (2026-09-25)

Решения пользователя: база импортов — BCL-набор (как ImplicitUsings); рантайм режем в той же
работе; обратную совместимость старых скриптов ломать разрешено.

**Симптом-мотиватор:** пользователь вставил из completion `UserDetail` → CS0103 + hover пустой.
Причина: Roslyn предлагает типы из ссылок без импорта (VS-механика «unimported types»), но мы
не дописывали `using` при коммите — половина фичи.

**Грабли разведки (проверено эмпирически):**
- `SymbolCompletionItem` (Roslyn) — internal; `item.Properties` у unimported-элементов ПУСТЫЕ
  (зонд-тест) — публичного пути «элемент → символ» нет.
- `Compilation.GetSymbolsWithName` — только source-декларации (в скрипте их нет) → не годится.
- Решение: свой `CompletionTypeIndex` (Host/Services) — обход
  `compilation.SourceModule.ReferencedAssemblySymbols` → public-типы → словарь
  MetadataName → namespaces[]; строится лениво ОДИН раз на контекст
  (`ContextWorkspace.TypeIndex`, `GetTypeIndexAsync`).
- BlazorMonaco 3.5: `CompletionItem.AdditionalTextEdits : List<SingleEditOperation>`
  (`BlazorMonaco.Editor`, поля Range/Text) — форма совпадает с внутренней monaco
  (suggestController читает `edit.range`/`edit.text`), JS-костыли не нужны.

**Реализация:**
- `CompletionItemDto.AdditionalTextEdits` (Contracts) + `AdditionalTextEditDto {OffsetFrom, OffsetTo, NewText}`.
- `CompletionQueryService`: для элементов с type-тегами (`WellKnownTags.Class/Interface/Structure/Enum/Delegate`;
  осторожно — именно `Structure`, не `Struct`) — namespace из индекса; правка вставляется ТОЛЬКО
  когда имя однозначно (ровно один namespace во всех ссылках) и он не в effective imports
  (CompilationOptions.Usings + `using X;` в тексте, regex multiline); вставка `using X;\n` в offset 0.
- Фронт: маппинг в `SingleEditOperation` через существующий `OffsetsToRange`.
- **Рантайм + контекст урезаны до BCL-набора** (breaking, подтверждён): `FunctionNodeImpl.ScriptOptions`
  и `FunctionNodeContextProvider.Imports` = System, System.Collections.Generic, System.Linq,
  System.Threading.Tasks, System.Threading (убраны System.Text, Mars.Nodes.Core, namespace Node, DI).
  Зеркало рантайм↔редактор сохранено; старые скрипты на скрытых импортах лечатся явным using
  (вручную или через completion).
- Тесты (+4, всего 24/24): unimported-элемент несёт `using System.Text;\n` (0..0); импортированный
  `Console` — без правки; `using` в коде подавляет правку; «using + new StringBuilder()» компилируется
  без CS0103. `Mars.Nodes.Tests` 566/566 (урезка импортов тесты нод не сломала).

## Грабли

- **FlurlClient мутирует общий HttpClient**: конструктор `FlurlClient(HttpClient)` ставит
  `httpClient.Timeout = InfiniteTimeSpan`; после первого запроса HttpClient бросает на любой
  сеттер `net_http_operation_started`. Поэтому в WASM-хостах `IFlurlClient` — один eager-инстанс
  в Program.cs, зарегистрированный готовым; НИКАКИХ фабрик `sp => new FlurlClient(sharedHttp)`.
- **Submission-проект = РОВНО ОДИН документ** (см. доработки 2026-09-25): несколько документов
  в одном submission-проекте молча ломают completion для всех, кроме первого.
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
