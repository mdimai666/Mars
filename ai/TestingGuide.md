# TestingGuide — тестовый сьют Mars (гайд для агентов)

Канон по тестам: как запускать, как устроен харнесс, что отклонено. Механика E2E-прогона —
также `ai/E2ETestingGuide.md`. Общие команды — QWEN.md «Build & Test».

## Запуск

- Сьют — **xUnit v3 + Microsoft Testing Platform**: каждый тест-проект собирается как
  MTP-exe (`OutputType=Exe`) и запускается напрямую из `bin\Debug\net10.0`.
- `dotnet test` **не использовать**: MTP-драйвер заблокирован несовместимостью SDK 10.0.400
  (exit code 5, "Zero tests ran").
- Полный прогон: `pwsh -NoProfile -File test-all.ps1` (`-IncludeE2E` — плюс E2E/DockerImage,
  `-List` — только список проектов). Скрипт запускает exe с cwd = `bin\Debug\net10.0`
  (фикстуры пишут в bin, изолированно по проектам); логи — `%TEMP%\mars-test-runs\<timestamp>\`.
- Один проект: `dotnet build tests/<Проект>` затем `<Проект>.exe` из bin. Фильтр MTP —
  по пути (`-filter "/Assembly/Namespace/Class/*"`), не по `FullyQualifiedName`; можно несколько.

## Опциональные гейты (переменные окружения)

- `MARS_E2E_TESTS=1` — E2E (`tests/Mars.E2E.Tests`, `[E2EFact]`,
  `Mars.Test.AppHost/Attributes/E2EFactAttribute.cs`): Playwright + системный Edge
  (`channel: msedge`), Postgres-контейнер. Без переменной все тесты в Skip.
- `MARS_DOCKER_TESTS=1` — docker-тесты: `tests/Mars.Docker.Tests` (`[DockerFact]`, живой демон:
  pull/run/exec нод `docker.*`) и `tests/Mars.DockerImage.Tests` (`[DockerContainerFact]`,
  сборка образа + миграции, `Fixtures/MarsFixture.cs`). Тяжёлые — без явной просьбы пользователя
  не запускать.
- `ExternalServices.*` (`tests/ExternalServices.Integration.Tests`,
  `ExternalIntegrationFactAttribute`) — внешние сервисы, тоже opt-in.

## Харнесс

- **`tests/Mars.Test.AppHost`** (classlib, namespace сохранён `Mars.Integration.Tests.*`) —
  общий харнесс для ~6 интеграционных проектов и benchmarks:
  - `Common/ApplicationFixture.cs` — хост приложения + Postgres testcontainer + Respawn;
  - `Common/DatabaseFixture.cs`, `IDatabaseFixture` — БД-фикстура;
  - `ApplicationTests.cs` — база интеграционных тестов;
  - `Fixtures/E2EServerFixture.cs` — сервер для E2E;
  - `Common/ApplicationFixtureInMemoryDb.cs`, `InMemoryDatabaseFixture.cs` — **готовы, но не
    используются** (InMemory отклонён, см. «Отклонено»);
  - `Common/MarsTestLoggerGlobalSetup.cs` — module initializer логгера: `MarsLogger` остаётся
    статическим фасадом (first-wins через Interlocked); классы из контейнера получают
    `ILogger<T>`/`ILoggerFactory`.
- **`tests/Mars.Test.Common`** — `TestEntityRefs`: per-fixture каталог сид-сущностей своей БД
  (замена статическим словарям `EntitiesCustomize`), кастомизация AutoFixture через ctor.
- **Изоляция на тест (инвариант):** ctor `ApplicationTests` на каждый тест делает
  `DbFixture.Reset()` (Respawn, без Task.Delay) + `Seed()` (`MarsDbStartup.SeedData` + TestUser +
  инвалидация кэшей) + ResetMocks.
- `Mars.Integration.Tests` — одна serial-коллекция `MarsApp` на общем
  `ApplicationFixture`/Postgres; это осознанная модель, а не долг.

## Рецепты верификации

- **Правило: верификация точечная** — только тесты затронутых клиентов/эндпоинтов + сборка;
  не закладывать в планы baseline-прогоны всего сьюта и ручные сценарии.
- **Рендер публичного фронта** (Docker): `HandlebarsAppFrontTests` в
  `tests/Mars.SiteEngine.Integration.Tests`.
- **Лёгкие тесты фронта без Docker**: `FrontManagerTests`, `AdminFrontTemplateTests`,
  `WebTemplateServiceWatcherTests`, `RenderEngineRenderTests`, `HandlebarsEngineCacheTests`,
  `FrontRenderErrorTests`, `StarterFrontTemplatesTests`, `FrontTemplateServiceTests`,
  `AiFrontFilesToolsTests` — в `tests/Mars.Integration.Tests`.
- **Рендер админ-форм — только E2E** `CreatePostTests` / `EditUserPageTests`
  (`tests/Mars.E2E.Tests`): юниты подмену редактора в реестре и цикл рендера не ловят
  (прецедент 2026-09-10 — две регрессии поймал только E2E). Обязателен после правок реестров
  редакторов, рендерера форм, общих input-компонентов админки. Перед прогоном
  `dotnet build Mars.slnx` (WASM-бандл админки в E2E-проект не входит). Селекторы — FluentUI v5
  (`fluent-button[type='submit']`, `[name='x'] input` — shadow DOM, `.fluent-data-grid`).

## Грабли

- Testcontainers for .NET 4.13 собирает образы через legacy `/build` API — `RUN --mount=type=cache`
  недоступен, `ImageFromDockerfileBuilder` на корневом Dockerfile падает. Нужен образ из
  Dockerfile с cache mounts — собирать через `docker build` (Process), как в
  `Mars.DockerImage.Tests/Fixtures/MarsFixture.cs`.
- `DockerUnavailableException` — перезапустить Docker Desktop и подождать ~25 с.
- Остаточные статики в харнессе benign при serial-прогоне и сознательно не трогаются:
  `_tokenGenerator`/`s_bearerToken` в `ApplicationFixture` (общий RSA/JWT на процесс),
  статический `JsonSerializerOptions` в `FlurlExtensions`.

## Отклонено (не предлагать снова без нового решения пользователя)

- **Распараллеливание `Mars.Integration.Tests`** — закрыто 2026-09-09 как нерентабельное:
  потребовало бы N Postgres-фикстур (N контейнеров ~2 ГБ+ каждый) при стене ~69–89 с;
  пользователь: «скорость тестов не так критична». Serial Postgres-харнесс — осознанная модель.
- **InMemory-БД для CRUD-тестов** — «для CRUD не надо InMemory»: InMemory ≠ реляционный EF
  (ILike/jsonb/raw SQL не работают); классы, проверяющие БД, остаются на Postgres.
  `ApplicationFixtureInMemoryDb`/`InMemoryDatabaseFixture` хранить готовыми, не использовать;
  предлагать InMemory только для классов без БД и только при реальной причине (например CI-лимиты).
