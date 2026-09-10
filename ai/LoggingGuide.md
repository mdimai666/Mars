# Mars Logging Guide for Agent

Как устроено логирование Mars и как им управлять через конфигурацию. Гайд написан после
разбора ситуации «в data/logs нет логов установки плагина» (2026-09-02): файловый логгер
фильтруется СВОЕЙ секцией конфигурации, и общий `Logging:LogLevel` на него не влияет.

## Как устроено

### Провайдеры

- **Консоль** — стандартный провайдер .NET (WebApplicationBuilder добавляет по умолчанию;
  `ClearProviders`/`AddConsole`/Serilog в Mars.WebApp нет).
- **Файл** — `NReco.Logging.File` (алиас провайдера `File`), регистрируется в
  `src/Mars.WebApp/UseStartup/MarsParts/MarsStartupPartLogging.cs` (`MarsAddLogging`).
  - Путь: `data/logs/app_<yyyy>-<MM>-<dd>.log` относительно рабочей директории процесса.
  - Уровни берёт из конфигурации (`loggingBuilder.AddConfiguration(builder.Configuration)`),
    хардкода уровней нет — секции фильтрации задаются в appsettings.
  - Предохранитель: NReco открывает файл жадно; при ошибке (нет прав, файл залочен) файловый
    лог молча отключается, в консоль печатается `mars: file logging disabled (<msg>)` —
    процесс не умирает. Если файлового лога нет — ищи эту строку в консоли.

### Источники конфигурации (приоритет от низшего к высшему)

1. `appsettings.json` (базовый, коммитится).
2. `appsettings.Local.json` (dev, рядом с приложением; `ConfigureAppConfiguration`,
   `reloadOnChange: true`). В Docker вместо него — `config/appsettings.Production.json`
   (том, пишет setup-визард, `AddWizardConfigSource`).
3. Альтернатива: `-cfg <path>` в аргументах или env `MARS_CFG` — единственный файл конфигурации.
4. Переменные окружения (`ConnectionStrings__DefaultConnection` и т.п.) и cmd-аргументы.

Секции JSON мержатся: то, что не задано в более приоритетном файле, наследуется из базового.

## Уровни и секции

Базовые настройки в `appsettings.json`:

```json
"Logging": {
    "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Quartz.Impl.StdSchedulerFactory": "Warning",
        "Quartz.Core.QuartzScheduler": "Warning"
    },
    "File": {
        "LogLevel": { "Default": "Warning" },
        "RetentionDays": 30
    }
}
```

Правила:

- `Logging:LogLevel` — общий фильтр (консоль и провайдеры без собственной секции).
- `Logging:File:LogLevel` — фильтр **именно файлового** провайдера (по алиасу `File`).
  Без этой секции файл наследует базовую (`Default: Warning`) — консольный Debug НЕ
  означает Debug в файле. Это главный источник путаницы.
- `Logging:File:RetentionDays` — сколько дней хранить файлы логов.
- Фильтрация по категориям: ключ — префикс полного имени класса логгера
  (`"Mars.*": "Debug"`, `"Microsoft": "Warning"` и т.п.).
- Всё подключено с `reloadOnChange: true` — правки уровней применяются на лету,
  перезапуск не обязателен (но надёжнее перезапустить).

### Рекомендуемый дев-набор (appsettings.Local.json)

```json
"Logging": {
    "LogLevel": { "Default": "Debug" },
    "File": {
        "LogLevel": {
            "Default": "Information",
            "Mars.*": "Debug"
        }
    }
}
```

`Mars.*: Debug` даёт детали кода Марса (включая установщик/менеджер плагинов), а
`Default: Information` не топит файл в Debug-шуме ASP.NET Core/EF. Максимум всего —
`"Default": "Debug"` в `File:LogLevel`.

## Как писать логи в коде

- Инжектить `ILogger<T>` через DI; категория логгера = полное имя класса →
  `"Mars.*"` покрывает все логгеры модулей Mars.
- Structured logging: сообщение с плейсхолдерами `{Name}` и аргументами, НЕ строковая
  конкатенация (`_logger.LogDebug("Processing plugin from configuration: {Name}", name);`).
- Уровни: `Trace` < `Debug` < `Information` < `Warning` < `Error` < `Critical`.
  Загрузка/конфигурация плагинов — `Information` для ключевых событий
  (`=== Starting plugins configuration ===`), `Debug` для деталей (какие плагины найдены,
  какие сборки загружены, что пропущено).

## Диагностика плагинов

**Стартовая активность плагинов (конфигурация, загрузка, применение отложенных
удалений/замен) — только в консоль**: `PluginManager` создаётся до `Build()` и
логирует через отдельный Console-фабрик (`AddPlugins`), поэтому его записей в
`data/logs` нет — ищи в консоли запуска. **Операции плагинов (установка/удаление/
отключение) — в файл**: они идут через `ILogger<PluginService>` (DI), который уже
подключён к файловому провайдеру.

Категории:

- `Mars.Plugin.Services.PluginService` — операции: `installed from nuget`,
  `marked for deletion`, `enabled`/`disabled` (в файле).
- `Mars.Plugin.Services.PluginManager` — стартовая загрузка: `=== Starting plugins configuration ===`,
  `Pending delete applied for plugin`, ошибки загрузки сборок (**консоль**).
- `Mars.Plugin.Handlers.PluginNugetInstaller` — установка из nuget: резолв замыкания,
  какие пакеты/сборки копируются или пропускаются (детали — Debug; в файле).
- `Mars.Plugin.Front.WebAssemblyPluginFrontExtensions` — загрузка фронта плагинов в
  админке (WASM) — это клиентская сторона, не файловый лог сервера.
- `Mars.Plugin.*` — всё плагинное сразу.

## Грабли

- **Записи старта/удаления плагинов не в файле** — `PluginManager` логирует в консоль
  (создаётся до `Build()`); в файле — только операции через `PluginService` и установщики.
- **Файл молчит при консольном Debug** — нет `Logging:File:LogLevel` в appsettings.Local.json;
  наследуется `Warning` из базового. Дописать секцию `File`.
- **Не добавлять второй файловый провайдер** — файл уже настроен в `MarsAddLogging`;
  самодельный `LoggerFactory` с `AddFile` в другом месте даёт два провайдера в один файл
  (гонки/битые строки) и дублирует конфигурацию. Нужен логгер в файле — инжектить
  `ILogger<T>` через DI.
- **Нет файлового лога вообще** — в консоли строка `mars: file logging disabled` (ошибка
  открытия файла); либо `data/logs` не создан (создаётся лениво при первой записи).
- **Файл огромный** — `"Mars.*": "Debug"` заменён на `"Default": "Debug"` целиком; вернуть
  `Default: Information` + точечные категории.
- **Уровень не применился** — проверить приоритет источников: значение из
  `appsettings.json`/env могло перекрыть `appsettings.Local.json` (env всегда выше).
