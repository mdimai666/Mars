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
├─ File.Exists("appsettings.Local.json")?
│   ├─ НЕТ → SetupWizardHost.RunAsync(args)
│   │        ├─ Отдельный WebApplication (Razor Pages + Bootstrap 5)
│   │        ├─ /setup → Welcome → Database → Site → User → Complete
│   │        └─ SignalComplete() → хост останавливается
│   └─ ДА → продолжаем
├─ WebApplication.CreateBuilder(args)
├─ ConfigureBuilder() → полный DI
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

**SysOptions** (SiteUrl, SiteName, SiteDescription) записываются в БД через `OptionService.SaveOption()` в `AppDbContextSeedData.SeedFirstOption()`, а не в конфиг. Данные берутся из секции `Setup` конфига.

### AppFront Mode

- `HandlebarsTemplate` — отрисовка из базы (по умолчанию), путь к шаблону не нужен
- `HandlebarsTemplateStatic` — статические файлы, нужен `StaticPath` (по умолчанию `../client`)
- `None` — фронтенд отключён

## Ограничения

Wizard **не запускается** когда:
- `IsRunningInDocker = true` — Docker использует переменные окружения
- `IsTesting = true` — тесты используют TestContainers

## E2E тесты

`tests/Mars.E2E.Tests/Tests/SetupWizardTests.cs`:
- `InvalidConnection_ShouldShowError` — авто-валидация БД при «Далее»
- `TestConnection_ShouldShowError` — кнопка «Проверить подключение»
- `FullFlow_ShouldReachCompletePage` — полный flow: wizard (БД → Site → User) → login → users page

## Документация для пользователей

`docs/dev_docs/SetupWizard.md` — полная документация.
