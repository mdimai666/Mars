# Setup Wizard — контекст для агента

## Что это

WordPress-style мастер первоначальной настройки Mars. Запускается при первом запуске, когда `appsettings.Local.json` отсутствует.

## Задумка

Пользователь не должен редактировать JSON-конфиги вручную. При первом запуске:
1. Приложение обнаруживает отсутствие `appsettings.Local.json`
2. Запускается отдельный минимальный хост с wizard'ом (НЕ основное приложение)
3. Пользователь проходит 4 шага: БД → настройки сайта → админ → готово
4. Wizard записывает `appsettings.Local.json` и останавливается
5. Основное приложение стартует с новой конфигурацией

## Почему отдельный хост

Основное приложение Mars **требует БД для старта**:
- EF Core миграции
- Seed данных (роли, типы, пользователи)
- Nodes engine
- AppFront

Без конфигурации БД основное приложение падает. Wizard работает **без БД** — только записывает конфиг.

## Архитектура

```
Program.cs
├─ WebApplication.CreateBuilder(args)
├─ SetupWizardHost.ShouldRunWizard()?
│   (вне Docker: нет appsettings.Local.json;
│    в Docker: нет connection string в env/примонтированном конфиге/./config;
│    MARS_SETUP_WIZARD=0 отключает)
│   ├─ ДА → SetupWizardHost.RunAsync(args)
│   │        ├─ Отдельный WebApplication (Razor Pages + Bootstrap 5)
│   │        ├─ /setup → Welcome → Database → Site → User → Complete
│   │        └─ SignalComplete() → хост останавливается,
│   │           записанный конфиг добавляется в builder.Configuration
│   └─ НЕТ → продолжаем
├─ MarsWebAppStartup.ConfigureBuilder() → полный DI
│   (в Docker: + AddWizardConfigSource — config/appsettings.Production.json с тома)
├─ ConfigureApp() → миграции + seed + pipeline
└─ app.Run()
```

## Шаги wizard

1. **Welcome** (`/setup`) — приветствие
2. **Database** (`/setup/database`) — подключение к PostgreSQL, авто-валидация
3. **Site** (`/setup/site`) — SiteUrl (авто-заполнение из браузера), SiteName, SiteDescription, Logging level, AppFront mode
4. **User** (`/setup/user`) — создание администратора
5. **Complete** (`/setup/complete`) — запись конфига, запуск основного приложения

## Файлы

| Файл | Роль |
|------|------|
| `src/Mars.WebApp/Setup/SetupWizardHost.cs` | Отдельный хост. `StartAsync()` → ждёт `SignalComplete()` → `StopAsync()` |
| `src/Mars.WebApp/Setup/SetupService.cs` | Хранит промежуточные данные wizard'а, тест БД (Npgsql), запись `appsettings.Local.json` |
| `src/Mars.WebApp/Pages/Setup/` | Razor Pages: Welcome, Database, Site, User, Complete |
| `src/Mars.WebApp/Pages/Setup/_Layout.cshtml` | Layout wizard'а (Bootstrap 5, марсианские цвета #C1440E) |

## Flow после wizard

1. Complete page OnGet: вызывает `SetupService.WriteLocalConfig()` — записывает конфиг
2. JS вызывает `fetch('/setup/complete?handler=Finish')` → `SignalComplete()`
3. Wizard host останавливается
4. `Program.cs` продолжается → `CreateBuilder` → `ConfigureApp`
5. Основное приложение стартует (миграции, seed с admin из конфига)
6. JS в Complete page polling'ит `/` каждые 2 сек → redirect на `/dev/Login`

## Seed после wizard

`SeedUsers.cs` читает `Setup:AdminEmail`, `Setup:AdminPassword`, `Setup:AdminFirstName` из `appsettings.Local.json`. Если секции нет — fallback на хардкод.

## appsettings.Local.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Port=...;Database=...;Username=...;Password=..."
  },
  "Setup": {
    "AdminEmail": "admin@example.com",
    "AdminPassword": "password",
    "AdminFirstName": "Admin",
    "SiteUrl": "https://example.com",
    "SiteName": "My Site",
    "SiteDescription": "Site description"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AppFront": [
    {
      "Mode": "HandlebarsTemplate",
      "Path": "../client",
      "Url": ""
    }
  ]
}
```

**SiteSettings** (SiteUrl, SiteName, SiteDescription) записываются в БД через `OptionService.SaveOption()` в `SeedFirstOptionHandler` (Mars.Server, вызывается из `AppDbContextSeedData.SeedFirstOption()`), а не в конфиг. Данные берутся из секции `Setup` конфига.

### AppFront Mode

- `HandlebarsTemplate` — отрисовка из базы (по умолчанию), путь к шаблону не нужен
- `HandlebarsTemplateStatic` — статические файлы, нужен `StaticPath` (по умолчанию `../client`)
- `None` — фронтенд отключён

## Ограничения и Docker

Визард **не запускается** когда:
- `IsTesting = true` — тесты используют TestContainers
- `MARS_SETUP_WIZARD=0` — принудительное отключение
- уже есть конфигурация БД

### Поведение в Docker

Гейт в `Program.cs` (`SetupWizardHost.ShouldRunWizard`): в Docker учитываются только
явные источники — env-переменные, примонтированный `appsettings.Production.json`
и конфиг визарда `config/appsettings.Production.json` на томе `./config`.
Девелоперские дефолты из `appsettings.json` внутри образа конфигурацией не считаются
(иначе визард бы никогда не запускался).

- Итог визарда в Docker пишется в `config/appsettings.Production.json`
  (`SetupWizardHost.WizardConfigPath`) — монтируйте `./config:/app/config`,
  иначе конфиг не переживёт пересоздание контейнера.
- При старте этот файл подключается в `MarsWebAppStartup.ConfigureBuilder`
  (`AddWizardConfigSource`) перед последним env-источником: приоритет выше
  json-дефолтов образа, но env-переменные перекрывают его.
- Порт: приложение и визард слушают 80 (`"Urls": "http://+:80"` в appsettings.json
  перекрывает дефолтные 8080 базового образа).
- Автоматизация без визарда: `ConnectionStrings__DefaultConnection` +
  `Setup__AdminEmail/AdminPassword/AdminFirstName`, `Setup__SiteUrl/SiteName/SiteDescription`,
  `Setup__FrontChoice` — всё читается через IConfiguration, env работает как есть.
- Безопасность: пока установка не завершена, UI визарда доступен без авторизации —
  в доках предупреждать не выставлять порт до первой настройки.

## E2E тесты

`tests/Mars.E2E.Tests/Tests/SetupWizardTests.cs`:
- `InvalidConnection_ShouldShowError` — авто-валидация БД при «Далее»
- `TestConnection_ShouldShowError` — кнопка «Проверить подключение»
- `FullFlow_ShouldReachCompletePage` — полный flow: wizard (БД → Site → User) → login → users page

## Документация для пользователей

`docs/dev_docs/SetupWizard.md` — полная документация.
