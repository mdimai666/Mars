---

# Mars NuGet Publish Guide for Agent

Как устроена публикация NuGet-пакетов Mars и как выпускать релиз. Гайд написан по итогам
настройки деплоя 2026-08-31 (переход с локального скрипта на публикацию из репозитория
через Trusted Publishing).

## Как организовано

### Версия — один источник

- `MarsAppVersion` объявлен в `Directory.Build.props` (корень репо). Там же задан
  `<Version>$(MarsAppVersion)</Version>` — **все** проекты решения получают версию приложения автоматически.
- Модуль/библиотека со своей версией — переопределяет `<Version>` в своём csproj (pack сам
  возьмёт его в `PackageVersion`).
- **Не** объявлять `MarsAppVersion` в `Directory.Packages.props`: порядок импорта MSBuild
  таков, что `Directory.Build.props` грузится раньше, и инъекция оттуда не работает
  (проверено эмпирически через `dotnet msbuild -pp`).

### Общие pack-метаданные — в `Directory.Build.props`

Авторы, иконка, SourceLink, snupkg, лицензия и т.п. заданы один раз и не дублируются в csproj:

- `<None Include="$(MSBuildThisFileDirectory)assets\icon-nuget.png" Pack="true">` — путь от корня, работает на любой глубине проекта.
- `<PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />` — детерминированная сборка + ссылка на коммит.
- `IncludeSymbols`/`SymbolPackageFormat=snupkg` — символы публикуются отдельным snupkg.

В csproj остаётся минимум: `<PackageId>`, `<Description>` (+ опционально `Product`/`PackageTags`/`Version`).

### Поверхность пакетов — авто-обнаружение по `<PackageId>`

CI и локальный скрипт пакуют **все проекты `src/**` (рекурсивно), у которых в csproj объявлен `<PackageId>`**
(на момент настройки — 54 шт: все `*.Contracts`/`*.Abstractions` + ранее публиковавшиеся библиотеки).

- **Правило транзитивности:** любой проект, на который ссылается пакуемый, обязан иметь
  `PackageId` — иначе в nuspec уйдёт зависимость по AssemblyName (битая ссылка).
  Пример: `Plugin.Kit.Front → Mars.Admin.Framework`.
- **Host-проекты модулей** (`Mars.X.Host`) и **серверное ядро с data-обвязкой** (`Mars.Server`,
  `Mars.Data.Repositories`) намеренно НЕ пакуются: они собираются только в композиции `Mars.WebApp`
  и вне её бесполезны. Не добавлять им `PackageId` (прецедент: `Mars.Server` и
  `Mars.Data.Repositories` были добавлены «по аналогии» при реструктуризации и исключены аудитом 2026-08-31).
- Новый пакет = просто добавить `<PackageId>` + `<Description>` в csproj; CI и скрипт подхватят сами.
- Приложения, тесты, бенчмарки и дев-стенды помечены `<IsPackable>false</IsPackable>` — защита
  от случайного `dotnet pack` (мусорных пакетов из WebApp/тестов).

### Каналы публикации

| Канал | Механизм | Когда |
|---|---|---|
| NuGet.org | GitHub Actions `.github/workflows/nuget-publish.yml`, Trusted Publishing (OIDC) | Релизы: тег `v*` или `workflow_dispatch` |
| Локальный фид | `pack-local-nugets.ps1` (корень репо) → `~/Documents/VisualStudio/_LocalNugets` (по умолчанию, переопределяется `-OutDir`) | Отладка, локальные зависимости |

## Как опубликовать релиз

1. **Поднять версию** — `MarsAppVersion` в `Directory.Build.props`
   (semver: `0.8.1-alpha.5` → `0.8.1-alpha.6` / `0.8.1` / `0.9.0`…).
   Если в релиз входят правки js/css, грузящихся в браузер — bump и так обязателен (cache-busting, см. `ai/CssRefactoringGuide.md`).
2. **Закоммитить и запушить** в `master`.
3. **Пометить тег** — тег обязан совпадать с `MarsAppVersion`:

   ```bash
   git tag v0.8.1-alpha.5
   git push origin v0.8.1-alpha.5
   ```

4. **Дождаться workflow** — Actions → «Publish NuGet packages» (запустится по тегу автоматически).
   Он: restore → pack всех проектов с `<PackageId>` (Release, `ContinuousIntegrationBuild=true`) →
   `NuGet/login@v1` (обмен OIDC-токена на одноразовый API-ключ) → push всех `*.nupkg`.
5. **Проверить nuget.org** — пакеты сначала проходят валидацию (обычно < 15 мин), в поиске
   появляются после индексации. При ошибке валидации придёт письмо на почту учётки.

Ручной запуск (без тега): Actions → «Publish NuGet packages» → **Run workflow** (ветка `master`).

## Настройка секрета и политики (разово, для человека)

Публикация идёт без долгоживущих API-ключей — через **Trusted Publishing**: GitHub Actions
получает OIDC-токен, nuget.org отдаёт временный (≈1 час) API-ключ. Нужны два шага:

1. **Политика на nuget.org** — залогиниться → верхнее правое меню → **Trusted Publishing** →
   создать политику:
   - Package owner: `mdimai666`
   - Repository owner / repository: `mdimai666` / `Mars`
   - Workflow file: `nuget-publish.yml` (имя файла в `.github/workflows/` — должно совпадать точно)
   - Environment: не задавать (если workflow не использует environments)
   - Репозиторий публичный — активация мгновенная (для приватных — 7-дневный бутстрап).
2. **Секрет в GitHub** — репозиторий `mdimai666/Mars` → Settings → Secrets and variables →
   Actions → **New repository secret**:
   - Имя: `NUGET_USER`
   - Значение: **имя профиля nuget.org (не email!)** — например `mdimai666`

### Про API-ключи (не делать)

NuGet.org ограничил срок API-ключей 30 днями (с 17.08.2026), все ключи, созданные до этой даты,
истекают 01.11.2026. Старый `NUGET_MARS` больше не используется — новые ключи не заводить,
публикация только через Trusted Publishing.

## Локальная упаковка (`pack-local-nugets.ps1`)

- Живёт в корне репо, пакует в `_LocalNugets` с `--include-source` и прогресс-баром.
- Путь фида по умолчанию — `~/Documents/VisualStudio/_LocalNugets` (через `$HOME`), переопределяется параметром `-OutDir`.
- Версию читает из `Directory.Build.props` (не из `Directory.Packages.props`).
- Список проектов не хардкодится — авто-обнаружение по `<PackageId>`, как в CI.

## Частые грабли

- **Битая зависимость:** пакуемый проект ссылается на проект без `PackageId` → nuspec получает
  зависимость по AssemblyName. Проверка: распаковать nupkg, в nuspec все `Mars*`-зависимости
  должны иметь префикс `mdimai666.`.
- **Пустые snupkg:** в Release `DebugType=none`, поэтому pack вызывается с
  `-p:DebugType=portable -p:DebugSymbols=true` (и в CI, и в скрипте).
- **Неверный путь к версии:** `MarsAppVersion` читается из `Directory.Build.props`
  (build-docker.ps1, publish-docker.ps1, pack-local-nugets.ps1) — не возвращать старое место.
- **Новый пакет «не публикуется»:** проверь, что `<PackageId>` реально в csproj (авто-обнаружение по нему).
- **`EnableDynamicLoading`:** держать только в plugin-цепочке (`src/Plugin/**` — сборки плагинов грузятся
  рантаймом: `Assembly.LoadFrom` в `PluginManager`, `LoadFromStream` в WASM-фронте). У серверных,
  контрактных и WASM-библиотек он не нужен (динамической загрузки нет — только статические ссылки).
