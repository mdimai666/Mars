# CliFnPlan — `mars fn`: одноразовый запуск C#-исходника в живом инстансе

CLI-команда для настройки среды, девопса и автоматизации: обычный C#-файл (top-level
statements) исполняется ОДИН раз в контексте уже запущенного приложения (полный DI —
достать сервис, добавить пользователя и т.п.), вывод — в консоль CLI, процесс завершается.

```
mars fn -f SomeFunction.cs
mars fn -f setup.cs -- arg1 arg2        # аргументы скрипту → Args
mars fn -c "Console.WriteLine(GetRequiredService<IUserService>().GetType());"
cat setup.cs | mars fn -f -             # stdin
```

## Решения (подтверждены пользователем 2026-09-25)

- **Новый модуль `Mars.CommandLine.Scripting`** — `src/Mars.Modules/` плоско, в slnx —
  виртуальная папка `/Mars.Modules/CommandLine/` (рядом с `Mars.CommandLine[.Abstractions]`).
- **Формы запуска — все четыре**: `-f/--file` (включая `-f -` = stdin), `-c/--code` (inline),
  trailing-аргументы после имени команды → `Args`. Ровно один источник кода, иначе ошибка валидации.
- **Globals — `FnContext`-обёртка**: `Services` (корневой `IServiceProvider`), `GetService<T>()`,
  `GetRequiredService<T>()`, `CreateScope()`, `Args`, `CancellationToken` — скрипт сразу «в контексте
  Mars», без обязательных using.
- **Без гейта доступа в v1**: UDS-сокет локальный, доверие как у `migrate`/`node inject`
  (кто имеет доступ к сокету — тот админ сервера). Фиксируем в доках команды.
- **Движок — Roslyn `CSharpScript`** (пин `Microsoft.CodeAnalysis.CSharp.Scripting` 5.9.0 УЖЕ есть
  в Directory.Packages.props — используется FunctionNode). .NET 10 file-based apps (`dotnet run x.cs`)
  НЕ подходят: отдельный процесс, нет доступа к DI живого инстанса.

## Опора на существующую механику (разведка 2026-09-25)

- `CommandCli` (System.CommandLine) + регистрация `app.Services.GetService<ICommandLineApi>()
  ?.Register<FnCommandCli>()` в Use-методе модуля (паттерн `MainNodes`/`MainIdentity`).
- Форвардинг живому инстансу через UDS получается БЕСПЛАТНО: `CliRemoteCommands` перехватывает
  `Console` и стримит вывод обратно, `CliRemoteClient.ExecAsync` возвращает **exit code** удалённой
  команды; при отсутствии сервера / `--local` — in-process исполнение. Ничего в канале менять не надо.
- Грабля: удалённые исполнения сериализуются (`_remoteInvocationLock`) — долгий скрипт держит
  очередь CLI. Приемлемо, отметить в доках.

## Состав

### 1. Модуль `src/Mars.Modules/Mars.CommandLine.Scripting/`

- csproj: `Microsoft.NET.Sdk`; PackageReference `Microsoft.CodeAnalysis.CSharp.Scripting`;
  ProjectReference → `Mars.CommandLine.Abstractions` (CommandCli/ICommandLineApi; Mars.Core
  придёт транзитивно — `OutResult(IUserActionResult)`).
- `FnContext` — globals-тип скрипта (список выше). Корневой провайдер + `CreateScope()`
  (для scoped-сервисов скрипт создаёт скоуп сам — явно или через хелпер).
- `FnScriptRunner` — компиляция+исполнение:
  - `ScriptOptions`: ссылки — **полный TPA** (`AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")`
    + сборки сервисов из `IServiceCollection`, как FunctionNodeImpl): одноразовый запуск,
    надёжность важнее скорости компиляции (~1–2 с приемлемо);
  - импорты — BCL-база как в FunctionNode/CodeCompletion (System, System.Collections.Generic,
    System.Linq, System.Threading.Tasks, System.Threading), остальное явными using в файле;
  - `CSharpScript.RunAsync(code, options, globals, ct)` → `ScriptState` (ReturnValue/Exception).
- `FnCommandCli : CommandCli` — команда `fn` ("run a C# file/inline code once inside the running
  Mars instance"): `Option<string> --file/-f`, `Option<string> --code/-c`, `Argument<string[]> args`;
  действие возвращает `Task<int>`:
  - чтение источника: файл (любое расширение, `.cs`/`.csx` не форсим) / `-f -` → stdin / inline;
  - `CompilationErrorException` → печать Diagnostics (`(строка,колонка): id сообщение`) → **exit 2**;
  - исключение времени выполнения → stderr (ToString) → **exit 1**;
  - `ReturnValue != null` → `Console.WriteLine(result)`; успех → **exit 0**.
- `MainCommandLineScripting`: `AddMarsCommandLineScripting(this IServiceCollection)` (регистрация
  раннера, если понадобится DI) + `UseMarsCommandLineScripting(this WebApplication app)` →
  `Register<FnCommandCli>()`.

### 2. Подключение

- `Mars.slnx`: проект в `/Mars.Modules/CommandLine/`.
- `MarsWebAppStartup`: ProjectReference в `Mars.WebApp.csproj` + `app.UseMarsCommandLineScripting()`
  в ConfigureApp **ДО** `UseMarsCliSocket` (удалённые исполнения лениво загружают типы команд
  один раз — к первому запросу дерево должно быть полным).
- `devstands/StandNodesApp`: НЕ подключаем — у стенда нет CLI-обвязки вообще
  (ни `ICommandLineApi`, ни `AddMarsCliSocket`), `Register` был бы no-op.
- `ICommandLineApi.InRemoteInvocation` (новое свойство, Abstractions) — командам нужно знать,
  что они исполняются по UDS (stdin/интерактив недоступны); реализация — `Remote.InRemoteInvocation`.
- Feature-флаг НЕ нужен (решение выше); JS/фронт не меняются — bump `MarsAppVersion` не нужен.

### 3. Тесты — `tests/Mars.CommandLine.Scripting.Tests` (xUnit v3 MTP, exe)

- Раннер: top-level код исполняется; `GetRequiredService<T>()` из фейкового DI; `Args` проброшены;
  ReturnValue возвращается; ошибка компиляции → diagnostics с номерами строк; runtime-исключение
  проброшено в ScriptState.Exception.
- CLI-форма: валидация источника (ровно один из -f/-c/stdin); `-f -` читает stdin.
- E2E (запуск WebApp + `fn` через сокет) — фаза 2, по паттернам `Mars.Cli.EndToEnd.Tests`.

### 4. Документация

- Этот план; по закрытии фичи — схлопывание в гайд по `ai/PlanLifecycleGuide.md`.
- Секция «Mars CLI» в QWEN.md уже говорит «командыdiscoverable через -h» — обновление не обязательно;
  публичные доки (`docs/dev_docs/`) — отдельным пунктом после проверки.

## Порядок работ (статус)

Ветка: `ai/cli-fn-scripting` (от `ai/node-rework-stage3-CodeCompletion`).

1. [x] Скелет модуля: csproj (пин CSharp.Scripting 5.9.0 уже был), slnx, `FnContext`,
   `FnScriptRunner`, `MainCommandLineScripting.UseMarsCommandLineScripting()`.
2. [x] `FnCommandCli`: `-f/--file` (включая `-f -` = stdin), `-c/--code`, trailing `args`,
   exit codes 0/1/2, печать diagnostics при ошибке компиляции. `ExecuteAsync`/`ReadSourceAsync`
   — internal static (тестируются без WebApplication), InternalsVisibleTo на тесты.
3. [x] Тесты `tests/Mars.CommandLine.Scripting.Tests` — **17/17** (раннер: return value,
   DI-сервисы (singleton+scoped через CreateScope), Args, BCL-импорты, compilation error
   с diagnostics, runtime-исключение в ScriptState.Exception; CLI: валидация источника,
   файл/stdin-remote, exit codes).
4. [x] Подключение WebApp; `dotnet build Mars.slnx` зелёный; `Mars.Cli.EndToEnd.Tests` 6/6
   (интерфейс `ICommandLineApi` расширен — единственная реализация `CommandLineApi`).
5. [ ] **Ручная проверка пользователем**: живой WebApp → `Mars.exe fn -c "return 2+2;"`,
   `-f файл.cs` (remote через UDS + `--local`), ошибка компиляции (exit 2), pipe+stdin,
   аргументы `-- a b`.

## Грабли (найдено при реализации 2026-09-25)

- **`CSharpScript.RunAsync` БЕЗ `catchException`-предиката перебрасывает runtime-исключения**
  скрипта, а не кладёт в `ScriptState.Exception` (тесты). Предикат есть только у
  `Script<T>.RunAsync` → схема: `CSharpScript.Create(code, options, globalsType)
  .RunAsync(globals, ex => ex is not OperationCanceledException, ct)` — отмену НЕ глотаем (Ctrl+C).
- **Script-парсер Roslyn не поддерживает `using var` declarations** на верхнем уровне скрипта
  (CS1002) — только классический блок `using (...) { }`. Актуально для доков/примеров `fn`
  и на будущее для FunctionNode-скриптов.
- **stdin в remote-исполнении недоступен**: сервер читает СВОЙ stdin, не клиентский
  (UDS-протокол stdin не форвардит). `-f -` при живом сервере — явная ошибка с подсказкой
  (`--local` или путь к файлу; сокет локальный, файл и так на той же машине).
- Ссылки скрипта — полный TPA через `MetadataReference.CreateFromFile` (пути, БЕЗ
  `Assembly.LoadFrom`) — сборки не грузятся в процесс до фактического использования.
