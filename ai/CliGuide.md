# CliGuide — Mars CLI: тонкий клиент к живому инстансу и `fn`

Гайд для агента: общая механика Mars CLI (UDS, root-флаги, quiet/JSON) и команда `fn`
(одноразовый запуск C#-скрипта в контексте живого приложения). История `fn` (план схлопнут):
`git show 3b13b36a:ai/CliFnPlan.md`.

**Карта страниц Blazor для агентов — отдельный гайд `ai/CliPagesGuide.md`**
(`Mars.exe pages list|show|find|layouts`: маршруты, роли, layout, исходники, код).
Типовой цикл: `pages find --url <url>` → `pages show <Name> --code` → правка.

## Где что лежит

- `src/Mars.Modules/Mars.CommandLine.Abstractions` — `CommandCli` (System.CommandLine),
  `ICommandLineApi` (в т.ч. `InRemoteInvocation`).
- `src/Mars.Modules/Mars.CommandLine` — `CommandLineApi`, `CliSocketServer`,
  `CliRemoteCommands`, `Remote/` (UDS-протокол: `CliProtocol`, `CliRemoteClient`,
  `MarsCliSocket`, `CliUrlsParser`), `Commands/StatusCommandCli`.
- `src/Mars.Modules/Mars.CommandLine.Scripting` — `FnCommandCli`, `FnContext`,
  `FnScriptRunner`, `MainCommandLineScripting`.
- Точка входа CLI — `src/Mars.WebApp` (Program.cs); команды модулей регистрируются
  в Use-методах (`Register<TCommandCli>`).
- Тесты: `tests/Mars.CommandLine.Scripting.Tests` (23), `tests/Mars.Cli.EndToEnd.Tests`.

## Как работает

- **Тонкий клиент**: если для текущего каталога запущен сервер, команда исполняется
  В ЖИВОМ процессе через unix domain socket (второй сервер не поднимается);
  `CliRemoteCommands` перехватывает Console и стримит вывод, `CliRemoteClient.ExecAsync`
  возвращает exit code удалённой команды. Без сервера / с `--local` — in-process.
- **Root-флаги — ДО команды**: `Mars.exe --quiet fn ...`, `--local`, `--no-uds`,
  `--disable-logs`. После подкоманды парсер падает «Нераспознанная команда».
- Удалённые исполнения сериализуются (`_remoteInvocationLock`) — долгий скрипт держит
  очередь CLI.
- В тестовом режиме (IsTesting / ASPNETCORE_ENVIRONMENT=Test) CLI-аргументы игнорируются.
- `--quiet`/`-q` — глобальный: глушит HelloText/инфо-таблицу, консоль-логи стартапа
  (прямые `Console.WriteLine` перехватываются на `TextWriter.Null`, фильтр
  `ConsoleLoggerProvider` ставится ДО `Build()`). **Грабля**: `PluginManager` логирует
  АВТОНОМНОЙ `LoggerFactory.Create(b => b.AddConsole())` — фильтры `builder.Logging`
  на неё не действуют; quiet пробрасывается конфигурационным ключом `Cli:Quiet`
  (in-memory), `AddPlugins` при нём создаёт фабрику без консоль-провайдера.

## `fn` — одноразовый C#-скрипт в живом инстансе

```
mars fn -f SomeFunction.cs            # файл (любое расширение)
mars fn -f setup.cs -- arg1 arg2      # аргументы скрипту → Args
mars fn -c "return GetRequiredService<IUserService>().GetType();"
mars -q fn --json -f setup.cs         # машинный конверт (ровно 2 строки stdout)
mars fn --examples                    # примеры форм запуска
```

- **Globals — `FnContext`**: `Services` (корневой IServiceProvider), `GetService<T>()`,
  `GetRequiredService<T>()`, `CreateScope()` (scoped-сервисы — через скоуп), `Args`,
  `CancellationToken`. Импорты — BCL-база (System, Collections.Generic, Linq, Tasks,
  Threading), остальное — явными using в файле.
- **Движок — Roslyn `CSharpScript`** (пин `Microsoft.CodeAnalysis.CSharp.Scripting`);
  ссылки — полный TPA (`AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")` + сборки
  сервисов из DI) через `MetadataReference.CreateFromFile` (без `Assembly.LoadFrom`).
  .NET 10 file-based apps не подходят: отдельный процесс, нет доступа к DI инстанса.
- **Exit codes**: 0 успех (ReturnValue печатается), 1 runtime-исключение (stderr) /
  usage-ошибка, 2 ошибка компиляции (diagnostics с номерами строк).
- **`--json`**: маркер `#MARS-FN-RESULT#` + одна JSON-строка
  `{"ok","exitCode","result","error"}`; конверт печатается и при ошибках; сырой
  ReturnValue в json-режиме не печатается; циклические ссылки → fallback `ToString()`.
- **Без гейта доступа**: UDS-сокет локальный, доверие как у `migrate`/`node inject`
  (кто имеет доступ к сокету — тот админ сервера).
- Подключение: `app.UseMarsCommandLineScripting()` в ConfigureApp ДО `UseMarsCliSocket`
  (дерево команд должно быть полным к первому удалённому запросу).

## Грабли

- **`CSharpScript.RunAsync` без `catchException`-предиката перебрасывает runtime-исключения**,
  а не кладёт в `ScriptState.Exception`. Схема: `CSharpScript.Create(...).RunAsync(globals,
  ex => ex is not OperationCanceledException, ct)` — отмену не глотаем.
- **Script-парсер Roslyn не поддерживает `using var`** на верхнем уровне (CS1002) —
  только классический блок `using (...) { }` (актуально и для FunctionNode).
- **stdin в remote-исполнении недоступен**: сервер читает СВОЙ stdin (UDS не форвардит).
  `-f -` при живом сервере — явная ошибка с подсказкой (`--local` или путь к файлу).
- Новая команда появится в CLI живого инстанса только после пересборки и рестарта сервера.

## Инварианты

- Команды мутируют ЖИВОЙ инстанс — запуск против работающего сервера только с
  подтверждения пользователя.
- Ровно один источник кода для `fn` (-f / -c / stdin), иначе ошибка валидации.
- Feature-флаг для `fn` не нужен; JS/фронт не меняются — bump MarsAppVersion не нужен.