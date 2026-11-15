# CliPagesGuide — `Mars.exe pages`: страницы Blazor для агентов

Команда `pages` отдаёт маршруты, роли, layout, пути к исходникам и код страниц админки —
без кроула DevTestPage и без grep по всему репо. Read-only, исполняется в живом инстансе
по UDS (тонкий клиент, второй сервер не поднимается). Добавлена 2026-11-12.

## Файлы

- Команда: `src/Admin/Mars.Admin.Host/CommandLine/PagesCommandCli.cs` (весь файл под `#if !NOADMIN`).
- Регистрация: `MainAdmin.UseMarsAdmin` → `app.Services.GetService<ICommandLineApi>()?.Register<PagesCommandCli>()`.
- Данные: `IBlazorPagesService` / `BlazorPagesService` (`src/Admin/Mars.Admin.Framework`) —
  тот же источник, что у DevTestPage (`/tech/devtest`); рефлексия по сборкам, кеш по сборкам.
- Тесты: `tests/Mars.Integration.Tests/CommandLine/PagesCommandCliTests.cs`
  (фильтры/резолв/дерево команд; инфраструктура не нужна).

## Подкоманды

```
Mars.exe pages list [--filter/-f regex] [--assembly/-a X] [--kind/-k page|layout|component|all] [--static] [--json]
Mars.exe pages show <имя|маршрут> [--code] [--json]     # карточка; --code печатает .razor + .razor.cs
Mars.exe pages find --url <url> [--code] [--json]       # GUID в URL матчатся под шаблоны {id:guid}
Mars.exe pages layouts [--json]                         # layout-ы + сколько страниц их используют
```

- `list` по умолчанию — только страницы с маршрутами (`--kind page`); компоненты без
  маршрутов (`CloudyTag`, `CloudyPageHead`…) — `--kind component`.
- `--static` — только маршруты без параметров `{...}`: по ним можно ходить напрямую
  (кроул, скриншоты, `tools/ui`).
- `show` резолвит: точное имя класса → точный маршрут → подстрока; несколько совпадений —
  таблица кандидатов, уточни запрос.
- В выводе `show`/`--json` маршруты даны и с префиксом маунта админки `/dev`
  (`/tech/devtest → /dev/tech/devtest`) — готовые URL для браузера/харнесса.
- Типовой агентский цикл: `pages find --url <url из бага>` → `pages show <Name> --code` → правка.

## Грабли

- **Корневые флаги — ДО команды**: `Mars.exe --quiet pages list`. После подкоманды
  (`pages show X --quiet`) парсер падает «Нераспознанная команда».
- **Абсолютные пути к исходникам — только Debug** (`Q.IsDevelopment`): сервис ищет корень
  репо по `Mars.slnx` + fallback-индекс файлов `src/`. В Release — относительный путь
  (`Mars.Admin/...`) и предупреждение при `--code`; fallback от cwd: `src/<relative>`.
- Сборки = `Mars.Admin` + Blazor-сборки из `AppDomain` (фронт-сборки плагинов грузит
  `PluginManager` в серверный процесс). WASM-список `WebAssemblyPluginFrontExtensions.PluginLoadAssemblies`
  на сервере НЕ используется. Microsoft/System-сборки отсекаются; assemblies с
  `ReflectionTypeLoadException` пропускаются.
- Живой инстанс должен быть собран с текущим кодом: новая подкоманда появится в CLI
  только после пересборки и рестарта сервера.
- Layout в карточке — из `LayoutAttribute`; наследование из `_Imports.razor` сервис
  не разворачивает (пустой Layout = дефолт ветки).
- Под NOADMIN команда не компилируется и не регистрируется.

## Рецепт: добавить подкоманду

1. В конструкторе `PagesCommandCli` — новый `Command` + опции, `SetAction`, `pagesCommand.Subcommands.Add`.
2. Логику, не требующую `app.Services`, выносить в `internal static`-хелперы
   (паттерн `FilterPages`/`ResolveSingle`/`GetSourceFilePaths`) — они тестируются без сервера.
3. Тесты в `PagesCommandCliTests`: хелперы + `CommandTree_ParsesWithoutErrors`
   (`new CommandLineApi(typeof(Program).Assembly, [])` + `new PagesCommandCli(api)`, парсинг без ошибок).
4. Проверка: `dotnet build Mars.slnx` +
   `tests\Mars.Integration.Tests\bin\Debug\net10.0\Mars.Integration.Tests.exe -class "Mars.Integration.Tests.CommandLine.PagesCommandCliTests"`.

## Отложено (согласовано с пользователем 2026-11-12)

- `pages tree` — группировка по Space/папкам.
- XActions страницы в `show` (привязки из `AdminXActionsOverlay`).
- Экспорт статических маршрутов в формате `tools/ui/assertions.json`.
