# Setup Wizard — контекст для агента

## Что это

WordPress-style мастер первоначальной настройки Mars. Запускается при первом запуске, когда `appsettings.Local.json` отсутствует.

## Задумка

Пользователь не должен редактировать JSON-конфиги вручную. При первом запуске:
1. Приложение обнаруживает отсутствие `appsettings.Local.json`
2. Запускается отдельный минимальный хост с wizard'ом (НЕ основное приложение)
3. Пользователь проходит 3 шага: БД → админ → готово
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
│   │        ├─ /setup → Welcome → Database → User → Complete
│   │        └─ SignalComplete() → хост останавливается
│   └─ ДА → продолжаем
├─ WebApplication.CreateBuilder(args)
├─ ConfigureBuilder() → полный DI
├─ ConfigureApp() → миграции + seed + pipeline
└─ app.Run()
```

## Файлы

| Файл | Роль |
|------|------|
| `src/Mars.WebApp/Setup/SetupWizardHost.cs` | Отдельный хост. `StartAsync()` → ждёт `SignalComplete()` → `StopAsync()` |
| `src/Mars.WebApp/Setup/SetupService.cs` | Тест подключения к БД (Npgsql), запись `appsettings.Local.json` |
| `src/Mars.WebApp/Pages/Setup/` | Razor Pages: Welcome, Database, User, Complete |
| `src/Mars.WebApp/Pages/Setup/_Layout.cshtml` | Layout wizard'а (Bootstrap 5, марсианские цвета #C1440E) |

## Flow после wizard

1. Complete page: JS вызывает `fetch('/setup/complete?handler=Finish')` → `SignalComplete()`
2. Wizard host останавливается
3. `Program.cs` продолжается → `CreateBuilder` → `ConfigureApp`
4. Основное приложение стартует (миграции, seed с admin из конфига)
5. JS в Complete page polling'ит `/` каждые 2 сек → redirect на `/dev/Login`

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
    "AdminFirstName": "Admin"
  }
}
```

## Ограничения

Wizard **не запускается** когда:
- `IsRunningInDocker = true` — Docker использует переменные окружения
- `IsTesting = true` — тесты используют TestContainers

## E2E тесты

`tests/Mars.E2E.Tests/Tests/SetupWizardTests.cs`:
- `InvalidConnection_ShouldShowError` — авто-валидация БД при «Далее»
- `TestConnection_ShouldShowError` — кнопка «Проверить подключение»
- `FullFlow_ShouldReachCompletePage` — полный flow: wizard → login → users page

## Документация для пользователей

`docs/dev_docs/SetupWizard.md` — полная документация.
