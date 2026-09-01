# План: реворк системы плагинов

> **Статус: все фазы (0–8) выполнены 2026-09-01; остался — каталог-сервер
> (отдельный репозиторий по `PluginServer/PluginCatalogPlan.md`).**
> Решения: 2026-08-31.
> Задача-источник: [Prompts/PluginSystemReworkPrompt.md](./Prompts/PluginSystemReworkPrompt.md).
> Связанные документы: [PluginServer/PluginDistributionPlan.md](./PluginServer/PluginDistributionPlan.md)
> (стратегия распространения), [PluginServer/PluginCatalogPlan.md](./PluginServer/PluginCatalogPlan.md)
> (каталог-сервер; этот реворк — фундамент его Фаз 4–5), [PluginCreationGuide.md](./PluginCreationGuide.md).

Цели реворка: установка плагинов из NuGet (все плагины в `data/plugins`), починка и
упрощение паковки (`PluginPublishScript`), ревизия именования/структуры `Mars.Plugin.*`,
единая роль `Mars.deps.json`, подготовка к маркетплейсу из `PluginDistributionPlan.md`.

## Принятые решения (2026-08-31)

1. **NuGet-пакет плагина — классический лейаут `lib/` + резолв при установке.**
   Пакет пакуется стандартным `dotnet pack` со всеми зависимостями в nuspec;
   `NuGetPluginInstaller` в Mars резолвит замыкание через NuGet.Protocol и копирует
   в `data/plugins/<id>/` только библиотеки, которых **нет** в замыкании Mars
   (фильтрация по `Mars.deps.json`). Как следствие: для nuget-потока автору плагина
   больше не нужна Release-акробатика `Private=false`/`ExcludeAssets=runtime`.
2. **Инструмент паковки — `Mars.Plugin.Sdk` как nuget с MSBuild-таргетами**
   (подробнее в разделе «Инструмент паковки»). Альтернатива (dotnet tool) описана
   там же — на случай, если таргет-модель не уживётся.
3. **Оба источника загрузки остаются** (секция `Plugins` в appsettings + `data/plugins`).
   Причина: в управляемом облаке понадобится принудительно внедрять плагины конфигурацией,
   без возможности удаления арендатором. Вводится политика источника: плагины из
   конфигурации — `Locked` (админка не даёт выключить/удалить).
4. **Все установленные плагины живут в `data/plugins/<packageId>/`** в единой раскладке,
   независимо от источника (zip / nuget / конфиг указывает на папку или сборку для dev).
5. **Изоляция через AssemblyLoadContext** — отдельная фаза этого плана (моя рекомендация,
   явного ответа не было; фаза автономна и может быть перенесена на отдельный план —
   но до её выполнения сохраняется риск конфликта версий сторонних зависимостей между
   плагинами, см. «Риски»).

## Как сейчас (as-is)

Загрузка:
- Два источника: секция `Plugins` appsettings (`PluginConfig{AssemblyPath, ContentRootPath}`)
  и скан `data/plugins` (детект папки по `<имя>.runtimeconfig.json` + `<имя>.dll`).
  `_имя` / `_папка` пропускаются. Загрузка только при старте, всё через `Assembly.LoadFrom`
  в дефолтном контексте — изоляции нет.
- `PluginManager` — `internal`, создаётся вручную в `ApplicationPluginExtensions.AddPlugins`,
  сам собирает `LoggerFactory` и `FileStorage` мимо DI.
- `app.UsePlugins()`: мапит каждый плагин в `/_plugin/<keyName>` (статика `wwwroot`,
  манифест `_front_plugins.json`, `/health`); `ApplyPluginMigrations()` вызывает
  `IPluginDatabaseMigrator` плагинов.
- Установка: `PluginController.UploadPlugin` → `PluginZipInstaller` — распаковка zip в
  `data/plugins/<имя файла>/` работает, но `InstallPlugin` — заглушка; установки из nuget нет.

Структура (9 проектов в `src/Plugin`, наслоение):
- `Mars.Plugin.PluginHost` — пустой (единственный файл целиком закомментирован).
- Дубль парсерных DTO: `Mars.Plugin/PluginProvider/Dto` и `Mars.Plugin.PluginPublishScript/Dto`
  (`DependiesJsonDto`, `ProjectDependencies`, `StaticwebassetsEndpointsManifestJson`).
- DTO/маппинги/`IPluginService` размазаны между `Mars.Plugin.Abstractions` и `Mars.Plugin.Contracts`.
- Опечатки в именах: `InstatitePlugin`, `DependiesJsonDto`, `Preapare`.
- Мёртвый код: приватные `MarsDeps()`/`MarsEndpoints()` в `PluginManifestProvider`
  (никогда не вызываются), `PluginExampleData` под `#if USE_EXAMPLE_PLUGINS`,
  `isDebug = true` хардкодом в `GenerateManifest`.
- `NuspecHelper` не используется в рантайме, но на него есть тесты — задел для паковки.

`Mars.deps.json` (замыкание сборок Mars.WebApp):
- Закоммичен в `src/Plugin/Mars.Plugin.PluginPublishScript/Mars.deps.json`, копируется из
  bin WebApp только в Debug-сборке, пакуется в nuget скрипта → вечно отстаёт от реальной
  версии Mars.
- «Что принадлежит Mars» задублировано хардкод-списком `MarsNugetsDefinition`
  в `PreparePublishData` — обновляется вручную.
- Используется только скриптом паковки; на рантайме фильтрация фронт-ассетов идёт по
  `Mars.Admin.staticwebassets.endpoints.json` рядом с `Mars.Plugin.dll`
  (`PluginManifestProvider.MarsDevAdminEndpoints`, бросает `FileNotFoundException` при отсутствии).

`Mars.Plugin.PluginPublishScript` (экспериментальный):
- Консольный exe, вызывается из csproj плагина таргетами `AfterTargets="Publish"/"CoreBuild"`
  с хардкодом пути и версии в команде (`$(NuGetPackageRoot)...\0.6.2-alpha.25\...` в README,
  `0.7.4-alpha.10` в шаблоне `MyMarsPlugin`); в `MyMarsPlugin.csproj` таргеты отключены
  (`AfterTargets="111Publish"` / `"111CoreBuild"`). README: «Костыль. Потом переделать»,
  в коде `//TODO: публикация сломалась`.
- Делает: отсечение сборок из замыкания Mars из publish-вывода, чистку
  `wwwroot/_framework` и `_content`, генерацию `_front_plugins.json`, сборку фактического
  содержимого будущего zip.

Фронт (админка, WASM):
- `WebAssemblyPluginFrontExtensions.AddRemotePluginAssemblies`: `/api/Plugin/RuntimePluginManifests`
  → манифест каждого плагина → скачивание перечисленных `.wasm` → запуск
  `IWebAssemblyPluginFront`. Баг: каждая сборка грузится дважды
  (`Assembly.Load(bytes)` и следом `AssemblyLoadContext.Default.LoadFromStream` тех же байтов).
- Есть предохранитель `?safe=1` при ошибке загрузки.

Найденные баги (фиксятся в Фазе 0):
- Таргет `CopyAppAdminStaticWebAssets` в `Mars.WebApp.csproj` копирует при publish файл
  под старым именем `AppAdmin.staticwebassets.endpoints.json`; WASM-проект после
  переименования называется `Mars.Admin` и генерирует `Mars.Admin.staticwebassets.endpoints.json`
  (проверено: `AssemblyName` не переопределён) → в релизной сборке файла нет,
  `PluginManifestProvider` при плагинах со статикой уронит старт.
- Двойная загрузка wasm (выше).

## Целевая архитектура

### Проекты: 9 → 6

| Было | Становится |
|---|---|
| `Mars.Plugin.Abstractions` | API для автора плагина: `MarsPlugin` (бывш. `WebApplicationPlugin`), атрибут, `PluginSettings`, `IPluginDatabaseMigrator` |
| `Mars.Plugin.Contracts` | Все DTO/маппинги/ответы/опции (забирает своё из Abstractions) |
| `Mars.Plugin` | Хост: `PluginManager`, загрузчики, инсталлеры (Zip + NuGet), `PluginManifestProvider` |
| `Mars.Plugin.Front.Abstractions` | Манифестные модели, общие серверу, скрипту паковки и WASM (`MarsFrontPluginManifest` + DTO `deps.json`/endpoints — сюда же, одно место вместо двух копий) |
| `Mars.Plugin.Front` | WASM-загрузчик плагинов админки |
| `Mars.Plugin.Kit.Host` / `Mars.Plugin.Kit.Front` | Остаются — мета-пакеты, точка входа автора плагинов |
| `Mars.Plugin.PluginHost` | Удаляется (пустой) |
| `Mars.Plugin.PluginPublishScript` | Переименовывается в `Mars.Plugin.Sdk` (см. ниже) |

Именование:
- `WebApplicationPlugin` → `MarsPlugin`, `WebApplicationPluginAttribute` → `MarsPluginAttribute`.
  Итог (2026-09-01): старые имена удалены без шимов — своих плагинов мало
  (`MyMarsPlugin`, `TelegramPlugin`, `PlayAudioNodePlugin`), все обновляемы.
- Опечатки: `InstatitePlugin` → `InstantiatePlugin`, `DependiesJsonDto` → `DependenciesJsonDto`.
- `PluginData` → `LoadedPlugin` (внутреннее). `PluginInfo`/`PluginConfig` остаются.

### Один источник манифестов Mars вместо коммиченного `Mars.deps.json`

При сборке/публике Mars генерируются два артефакта:
- `Mars.deps.json` — замыкание сборок WebApp (отсечение общих сборок; фильтрация при
  установке из nuget; «шаренный с хостом» набор для ALC);
- `Mars.Admin.staticwebassets.endpoints.json` — статика админки (фильтрация фронт-ассетов
  плагинов на рантайме — какие библиотеки фронту грузить самому).

Оба пакуются в nuget `mdimai666.Mars.Plugin.Sdk` **той же версии, что и весь релиз Mars**
(`MarsAppVersion` из `Directory.Build.props`). Ничего не коммитится в репо;
хардкод-список `MarsNugetsDefinition` удаляется — источник правды один и всегда в версии релиза.

### Инструмент паковки — `mdimai666.Mars.Plugin.Sdk` (выбранный вариант)

Один консольный инструмент в пакете: `tools/Mars.Plugin.Sdk.dll` + `build/*.targets`.
Автор плагина добавляет:

```xml
<PackageReference Include="mdimai666.Mars.Plugin.Sdk" Version="..." PrivateAssets="all" />
```

Таргеты вызывают инструмент через `$(Pkgmdimai666_Mars_Plugin_Sdk)` — свойство, которое
NuGet сам генерирует для прямого PackageReference: путь и версия берутся из рестора,
хардкод версии в теле таргета исключён (главная болезнь текущего скрипта).

Команды инструмента:
- `pack zip` (после `dotnet publish`, Release): отсекает сборки из замыкания Mars,
  чистит `wwwroot/_framework`/`_content`, генерирует `_front_plugins.json`, собирает zip;
- `pack nuget` (после `dotnet pack`): допаковывает в nupkg фронт-ассеты плагина
  (очищенные от общих с Mars) в папку пакета (конвенция `mars/front/`) и метаданные
  (см. конвенции ниже).

Метаданные/конвенции пакета (Фаза 0 `PluginDistributionPlan`):
- `<packageType>MarsPlugin</packageType>` — чистый фильтр на nuget.org;
- `marsVersionMin`/`marsVersionMax` (nuspec/`AssemblyMetadata`) — совместимость с хостом,
  проверяется при установке и при загрузке (модель `engines` из VS Code, HACS/Obsidian);
- entry-сборка — по конвенции `<PackageId>.dll` либо явное поле;
- при установке любой источник материализует в папке плагина `mars-plugin.json`
  (id, версия, источник, совместимость, дата) — рантайм читает только его.

**Альтернатива (записана по просьбе пользователя): `dotnet tool`.** Тот же инструмент
как `mdimai666.mars-plugin-sdk`, вызов явными командами `dotnet mars-plugin pack zip|nuget`,
версия — в `.config/dotnet-tools.json` репо плагина (`dotnet tool restore` на любой машине/в CI).
Плюсы: прозрачность, дебаг, нет магии в сборке; минус: нет «кнопки Publish» в VS —
только терминал/CI. Переключение на этот вариант = смена обвязки вызова, ядро то же.

### Установка и раскладка

Единая раскладка `data/plugins/<packageId>/`: entry-dll, сторонние зависимости (только те,
которых нет в замыкании Mars), `wwwroot/`, `mars-plugin.json`, собственный `.deps.json`.

- `ZipInstaller`: закрывает заглушку `InstallPlugin` — валидация структуры, проверка
  совместимости по `marsVersionMin/Max`, запись раскладки. Самокомплектный zip
  (отсечение при паковке обязательно — резолвить при установке из zip неоткуда).
- `NuGetPluginInstaller` (NuGet.Protocol): источники настраиваются (по умолчанию
  nuget.org; список открыт для добавления своего фида — задел под BaGet, Фаза 3
  `PluginDistributionPlan`); скачивание пакета + резолв замыкания; копирование только
  отсутствующих в Mars библиотек; раскладка фронт-ассетов из `mars/front/`;
  материализация `mars-plugin.json`. Проверка `packageType=MarsPlugin`.
- Оба инсталлера используют общую логику раскладки.
- Установка — роль `Admin`/`Developer`, с подтверждением в админке; результат —
  «установлено, требуется рестарт» (плагины грузятся при старте; механизм рестарта
  уточняется при реализации: если готового нет — сообщение админу).
- **Blocklist** (опция, модель n8n): запрещённые `packageId` не ставятся и не грузятся —
  защита без каталога.
- Обновление = скачивание новой версии → замена папки → рестарт; откат = установка
  прежней версии (резервных копий в v1 не держим).

### Политика источников (облачный сценарий)

- `Source = Config` (секция `Plugins` appsettings): принудительные плагины инстанса;
  по умолчанию `Locked` — админка не показывает выключение/удаление; управление только
  конфигурацией (нужно для managed-облака).
- `Source = Zip | NuGet`: обычные, управляются админом.
- Источник и `Locked` видны в списке плагинов.

### Изоляция (фаза про ALC)

Свой `AssemblyLoadContext` + `AssemblyDependencyResolver` (по `.deps.json` плагина) на
каждый плагин — стандартный паттерн «приложение с плагинами»: сборки из замыкания Mars
резолвятся из дефолтного контекста (тип-идентичность с хостом), остальное — из папки
плагина. Это делает безопасными разные версии сторонних библиотек у разных плагинов
и является предусловием будущей выгрузки/горячей замены.

### Жизненный цикл (практики популярных продуктов)

| Заимствуем | Откуда |
|---|---|
| install / activate / deactivate / delete; данные не удаляются при деинсталляции | WordPress |
| проверка совместимости с версией хоста до установки (`engines`) | VS Code, Obsidian, HACS |
| blocklist вредоносных, verified-уровни | n8n |
| подпись пакетов (позже, вместе с «рекомендовано») | Grafana, NuGet signed packages |
| плагин = nuget-пакет, установка из админки | Umbraco 13+, Orchard Core |

В админке (`PluginsListPage` + новые диалоги): статусы установлен/включён/выключен/
несовместим/есть обновление, источник, кнопки управления по политике, установка по
NuGet-id и из zip. Отключение = пропуск загрузки при следующем старте (флаг хранится
через существующую систему Options, ключ = `packageId`).

## Фазы

Каждая фаза — самостоятельный коммит-набор; после каждой решение собирается и работает.

### Фаза 0. Быстрые фиксы найденных багов — **выполнена 2026-09-01**

- ~~`CopyAppAdminStaticWebAssets` → копирование `Mars.Admin.staticwebassets.endpoints.json`~~
  Таргет переименован в `CopyMarsAdminStaticWebAssets`, имя файла исправлено
  (эмпирически подтверждено: файл генерируется сборкой `Mars.Admin` в OutputPath WebApp;
  старый `AppAdmin.*` не существовал нигде → таргет молча не исполнялся).
- ~~Убрать двойную загрузку в `WebAssemblyPluginFrontExtensions`~~ — оставлен один
  `AssemblyLoadContext.Default.LoadFromStream`.
- ~~`isDebug` в манифесте~~ — `app.Environment.IsDevelopment()`.

Проверено: сборка `Mars.slnx` зелёная; `Mars.Plugin.Tests` (3/3) и
`Mars.Plugin.Integration.Tests` (7/7, `PluginExample` проходит полный пайплайн);
в публикации файл эндпоинтов лежит рядом с `Mars.dll`.

### Фаза 1. Структура и именование — **выполнена 2026-09-01**

Сделано:
- Удалён пустой `Mars.Plugin.PluginHost` (ссылки из `Kit.Host` и `Mars.slnx` убраны;
  киты остаются — пользователь подтвердил роль кита как «готового комплекта»).
- Парсерные модели (`deps.json`, endpoints-json) переехали из дублей
  (`Mars.Plugin/PluginProvider/Dto` и `Mars.Plugin.Sdk/Dto`) в одно место —
  `Mars.Plugin.Front.Abstractions`; опечатка имени файла `DependiesJsonDto` устранена
  (класс уже назывался правильно).
- **Отклонение от плана:** DTO/маппинги/`IPluginService` НЕ переносились в
  `Mars.Plugin.Contracts` — текущее размещение в `Abstractions` совпадает с конвенцией
  модулей (`Mars.Options.Abstractions`, `Mars.Docker.Abstractions`: сервисные DTO живут
  в `*.Abstractions`, HTTP-модели — в `*.Contracts`).
- `PluginManager` — зависимости в конструкторе (`ILogger<PluginManager>`,
  keyed-`IFileStorage("data")` из уже зарегистрированных сервисов, без сборки
  провайдера); ручной `FileStorage`/`LoggerFactory` внутри удалены; для хостов без
  `MainServer` keyed-хранилище регистрируется в `AddPlugins` тем же экземпляром.
- Переименования: `WebApplicationPlugin`/`WebApplicationPluginAttribute` →
  `MarsPlugin`/`MarsPluginAttribute` (без шимов — удалены по решению пользователя
  2026-09-01), `PluginData` → `LoadedPlugin`, `InstatitePlugin` → `InstantiatePlugin`;
  `PluginExample` и шаблон `MyMarsPlugin` переведены на новые имена.
- Мёртвый код: `PluginExampleData` + ветки `#if USE_EXAMPLE_PLUGINS` удалены,
  комментированный дамп атрибутов из `PluginInfo` убран.
  **Отклонение:** `NuspecHelper` НЕ удалён/не перенесён — его использует
  `UploadPluginTests` (генерация тестового nuspec); пригодится в Фазе 3.
- Перезаписан `README` `Mars.Plugin` под реальное API.

Проверено: `dotnet build Mars.slnx` зелёная; `Mars.Plugin.Tests` 3/3 (+1 скип),
`Mars.Plugin.Integration.Tests` 7/7 (PluginExample грузится по новому атрибуту,
миграции применяются), точечные `GetPluginTests|UploadPluginTests` 6/6.

### Фаза 2. Манифесты и `Mars.Plugin.Sdk` — **выполнена 2026-09-01**

Сделано:
- Проект переименован `Mars.Plugin.PluginPublishScript` → `Mars.Plugin.Sdk`
  (`mdimai666.Mars.Plugin.Sdk`); коммиченный `Mars.deps.json` удалён, пакуется из вывода
  сборки `Mars.WebApp` при паке (ошибка, если решение не собрано);
  `MarsNugetsDefinition` заменён проверкой префикса `mdimai666.*`.
- Пакет несёт: `tools/` (инструмент + `Mars.deps.json`), `mars/` (оба манифеста),
  `build/mdimai666.Mars.Plugin.Sdk.targets` (автоимпорт, путь через `$(Pkg...)`).
- Команды инструмента: `pack zip` (стрип + манифест + дескриптор + zip, автоматически
  после `publish -c Release`), `pack nuget` (классический лейаут `lib/` + `mars/front`,
  зависимости в nuspec, `packageType=MarsPlugin` — таргет `MarsPluginPackNuget`),
  `debug-manifest` (после Debug-сборки). Дескриптор `mars-plugin.json`
  (id/версия/entry/`MarsVersion`) пишется в обоих потоках.
- Исправлены корневые причины «публикация сломалась»: стрип удалял
  `<Плагин>.staticwebassets.endpoints.json`, который на рантайме читает
  `PluginManifestProvider` (теперь сохраняется); скан фронт-сборок падал
  `ReflectionTypeLoadException` на отсутствующих в папке плагина сборках Марса
  (теперь берётся загружаемое подмножество типов).
- Мёртвые `MarsDeps()`/`MarsEndpoints()` и серверный дубль `ProjectDependencies` удалены.
- Шаблон `MyMarsPlugin` переведён на Sdk (старые отключённые таргеты с хардкодом версий
  удалены); `PluginExample` не пакуется — менять нечего. В `nuget-publish.yml` и
  `pack-local-nugets.ps1` добавлена сборка решения перед паком.

Грабли (зафиксированы в csproj/таргетах): пустой snupkg в Release роняет пак `NU5017`
(`IncludeSymbols=false`); tools-only пакет требует `IsTool`/`IncludeBuildOutput=false`;
многострочные свойства ломают `Exec` — санитизация в таргетах. Известные недочёты:
в папке плагина остаются общие с админкой `_framework/*.wasm` (мёртвый вес, рантайм-фильтр
их не отдаёт; паковое отсечение — позже); пак из полностью чистого состояния одним
`dotnet pack` нестабилен — всегда сначала сборка, затем пак (в доках и скриптах так и есть).

Проверено: пакет в `_LocalNugets` версии релиза; на `MyMarsPlugin` работают publish→zip,
`MarsPluginPackNuget`→nupkg, Debug-манифест; сборка `Mars.slnx` и `Mars.Plugin.Tests` зелёные.

### Фаза 3. Раскладка, инсталлеры, nuget-установка — **выполнена 2026-09-01**

- `mars-plugin.json` + единая раскладка; детект папок по дескриптору
  (обратная совместимость с `.runtimeconfig.json`).
- Общая логика раскладки; `ZipInstaller.InstallPlugin` реализован (валидация +
  проверка совместимости).
- `NuGetPluginInstaller` (NuGet.Protocol): источники (nuget.org по умолчанию +
  настраиваемый список), резолв замыкания, фильтрация по `Mars.deps.json`,
  раскладка фронт-ассетов.
- Опции: источники, blocklist. REST: `Install(packageId)` / upload zip; клиент
  `Mars.WebApiClient`; роль `Admin`/`Developer`; ответ «установлено, нужен рестарт».

**Готово когда**: тестовый плагин ставится и из локального фида `_LocalNugets`, и с
nuget.org (тестовый пакет), раскладывается в `data/plugins`, подхватывается после
рестарта; banned-по-blocklist пакет не ставится.

**Итог:** реализованы `PluginPackageDescriptor` (общая модель в `Abstractions`), детект
папок по дескриптору в `PluginManager`, валидация+переименование в `<PackageId>` в
`PluginZipInstaller`, `PluginNugetInstaller` (NuGet.Protocol: резолв транзитивных
зависимостей, фильтр по замыканию из `Mars.deps.json`, раскладка `lib/`→корень и
`mars/front`→`wwwroot`), опции `NugetSources`/`BlockedPackageIds`, эндпоинт
`InstallFromNuget` + клиент `Mars.WebApiClient`.

Проверено тестами (`Mars.Plugin.Tests`, 21/21): раскладка из локального фида, «не
плагин» отклоняется, резолв зависимостей и фильтр марсовых сборок, детект папок по
дескриптору. Установка с nuget.org проверяется тем же кодом (источник — настраиваемый
список; реального опубликованного пакета плагинов пока нет, появится с каталогом).

Ограничение: фильтр при установке берёт замыкание из `Mars.deps.json` рядом с
приложением (в публикации/`bin` он есть); при его отсутствии зависимости ставятся без
фильтра (лог). Блок-лист проверяется при установке из nuget; при загрузке из папок —
пока нет (опции недоступны до `Build()`).

### Фаза 4. Изоляция через AssemblyLoadContext — **выполнена 2026-09-01**

- Новый `PluginLoadContext` (ALC + `AssemblyDependencyResolver` + фолбэк «файл рядом
  с входной сборкой»): сборки Марса из `Mars.deps.json` резолвятся из дефолтного
  контекста (тип-идентичность с хостом), остальное — из папки плагина.
  В `InstantiatePlugin` вместо `Assembly.LoadFrom` — `LoadFromAssemblyPath` в ALC.
- `PluginAssemblyHelper.ReadFrontAssemblies` — резолв фронт-сборок по физическому пути
  (вместо `Assembly.Load` по имени); `PluginManifestProvider` читает endpoints-json из
  `ContentRootPath` плагина (вместо `Location`).
- Интеграционный тест `PluginIsolationTests`: два плагина с разными версиями одной
  сторонней библиотеки (сборки компилируются на лету через Roslyn) — разные контексты,
  каждая версия своя; добавлен `Microsoft.CodeAnalysis.CSharp` в тестовый проект.

Проверено: `Mars.Plugin.Tests` 22/22 (+1 скип), `Mars.Plugin.Integration.Tests` 7/7
(PluginExample грузится через ALC, миграции работают), контроллерные 6/6, сборка
`Mars.slnx` зелёная.

Известное ограничение: фронт-сборки плагина (wasm) грузятся отдельным
`Assembly.LoadFrom` (не в ALC плагина) — они вне замыкания хоста и не конфликтуют.

### Фаза 5. Жизненный цикл и админка — **выполнена 2026-09-01**

- `PluginSource` (Config/Zip/NuGet) + реестр установленных `data/plugins/.registry.json`
  (источник, версия, дата, disabled): инсталлеры пишут, `PluginManager` читает
  (пропуск disabled при загрузке, источник в списке); конфиг-плагины — `Locked`
  (не отключаются/не удаляются из админки).
- Эндпоинты `POST api/Plugin/SetEnabled` / `Uninstall` (+ клиент WebApiClient),
  `IPluginService.SetEnabled/Uninstall`, `UserActionException` при Locked/не найден.
- Админка: бейджи источника/статуса в списке, кнопки Enable/Disable/Update/Delete
  (Update = переустановка последней версии из nuget), диалог установки по NuGet-id
  (`NugetInstallDialog`), меню «Install from NuGet». Заглушка `Delete` заменена.
- **Попутный фикс изоляции:** тестовое окружение определялось по переменной окружения
  процесса (в тестах пуста) — теперь по `builder.Environment.EnvironmentName == "Test"`;
  в тестах плагин грузится в default-контекст (type-identity для прямых обращений к
  типам плагина), в проде — ALC. Это чинит 5 интеграционных тестов
  (`PluginDbContextTests`, `PluginSetupTests`, `PluginOptionTests`), которые сломала
  Фаза 4.

Проверено: `Mars.Plugin.Tests` 26/26 (+1 скип), `Mars.Plugin.Integration.Tests` 7/7,
плагинные тесты `Mars.Integration.Tests` 9/9 (включая новые `PluginLifecycleTests`),
сборка `Mars.slnx` зелёная.

Ограничение: отключение/включение/удаление применяются после рестарта (загрузка при
старте); блок-лист при загрузке из папок не проверяется (опции недоступны до Build).

### Фаза 6. Документация и каталог — **выполнена 2026-09-01**

- `ai/PluginCreationGuide.md` — добавлен раздел «Install & Lifecycle (runtime)»
  (установка zip/nuget, Enable/Disable/Update/Delete, `Locked`, источники и блок-лист,
  изоляция ALC).
- `docs/dev_docs/Plugins/PluginSdk.md` — раздел «Установка и управление» вместо
  «Установка в Марс» (теперь актуально: установка из zip и nuget, жизненный цикл,
  изоляция); `PluginGettingStart.md` — раздел «Установка и обновление».

### Фаза 7. Чистка SDK: удаление `_front_plugins.json`, фикс классификации по замыканию — **выполнена 2026-09-01**

Два решения пользователя 2026-09-01 (по итогам обсуждения):

1. **`_front_plugins.json` не хранится в плагине, а генерируется сервером.**
   Файл, который SDK писал в `wwwroot` плагина при паковке, на рантайме никем не
   читается: сервер (`PluginManifestProvider.GenerateManifest` + `MapGet` в
   `PluginManager.UsePlugins`) собирает манифест на лету из `<Плагин>.staticwebassets
   .endpoints.json` с фильтром по `Mars.Admin.staticwebassets.endpoints.json`, и endpoint
   выигрывает у статик-файла. SDK-версия была рудиментом старого as-is-флоу.
   Удалены: `ManifestProcessing.cs` целиком (включая pack-time `FilterEndpoints`),
   команда `debug-manifest` (`ProcessMode.DebugManifest`, таргет `MarsPluginDebugManifest`),
   шаг `[3/4]` и `generateFilesNames` в `Program.cs`. Рантайм не тронут —
   `MarsFrontPluginManifest` в `Front.Abstractions` остаётся контрактом сервер↔WASM.
2. **Классификация «пакет Марса» — по замыканию, не по имени.** Префикс
   `mdimai666.*` (`PreparePublishData.IsMarsPackage`, `ProcessScriptSettings
   .CurrentScriptProjectNugetName`) ломался на плагинах автора, опубликованных под
   собственным именем: свои пакеты ошибочно стрипались из плагина. Заменён на
   членство в замыкании `Mars.deps.json` ∪ явный список аддонов (пакеты экосистемы
   вне замыкания WebApp: `Mars.Plugin.Kit.Host`, `Mars.Plugin.Kit.Front`, и сам SDK
   как dev-инструмент — `devTools` фильтрует по assembly name, а в deps.json ключ —
   package id). Это же правило уже работает на рантайме (`PluginNugetInstaller`, ALC) —
   паковка приведена в соответствие.
   Попутно: чистка мёртвого кода SDK (`EnvVa`, закомментированные блоки,
   неиспользуемые свойства DTO deps.json), обновление доков.
3. **Хотфикс по итогам проверки на реальном плагине (`Mars.PlayAudioNodePlugin`):
   `Kit.Host` в замыкании хоста.** Плагин в `ConfigureWebApplication` вызывал
   `AutoHostRegisterHelper` (расширение из `Mars.Plugin.Kit.Host`) и падал с
   `FileNotFoundException`: кит стрипается из папки плагина при паковке (аддон),
   но и в замыкании хоста его не было (никто не ссылался) → в ALC резолвиться
   было неоткуда. До реворка спасал `Assembly.LoadFrom` (дефолтный контекст
   зондировал папку плагина, старый скрипт кит не вырезал). Фикс: `Mars.Plugin`
   ссылается на `Mars.Plugin.Kit.Host` (хост-рантайм гарантирует доступность кита;
   сборка попадает в `Mars.deps.json`/замыкание и резолвится из дефолтного
   контекста). `PluginLoadContext.Load` для марсовых сборок усилен: вместо
   «вернуть, только если уже загружена» — всегда `null` (делегирование дефолтному
   контексту), что заодно не даёт неудалённой копии марсовой сборки из папки
   плагина попасть в плагинный контекст.
   Покрытие: `PluginExample` получил минимал-апи эндпоинты (`/api/PluginExample/Ping`,
   `/api/PluginExample/OptionValue` с DI-параметром; контроллер уже был) + новые
   тесты `PluginEndpointTests` (интеграционные) и регрессионный
   `PluginIsolationTests.Plugin_UsingKitHost_ResolvesItFromDefaultContext`.
4. **Пустой список встроенных звуков плагина после установки (`FileListUtility`).**
   `BuiltInSoundsService` читает `wwwroot/sounds` утилитой `FileListUtility.GetFiles`
   с `useRootGitIgnore: false`, но та при `false` обходила предков до корня диска
   и грузила ИХ `.gitignore`. Плагин после реворка лежит в
   `<хост>/src/Mars.WebApp/data/plugins/...` — паттерн `/data` из
   `src/Mars.WebApp/.gitignore` матчил относительный путь и игнорировал все ассеты
   (до реворка плагин жил в своём репо — паттерн не срабатывал). Фикс в утилите:
   без `useRootGitIgnore` `.gitignore` вне читаемой папки не загружаются (внутренние
   — по-прежнему да). Покрытие: `FileListUtilityTests` (3 кейса), `DirReadNodeTests`
   без регрессии.
5. **«Access to the path ... is denied» при установке из zip (строка финального
   `MoveDirectory`).** `Directory.Move(staging → final)` на Windows падает ровно
   этим сообщением, когда файл внутри переносимой папки удерживает чужой процесс
   (проверено эмпирически: существующее назначение даёт другое сообщение). Наш код
   хэндлы не держит (все потоки закрыты к переносу); держит антивирус/индексатор,
   сканирующий свежезапакованные `.dll` (репо в `Documents`), лок временный —
   повторная загрузка ставила плагин. Фикс: `MoveInstalledPluginAsync` — перенос
   с ретраями (экспоненциальные паузы, отмена по токену), при исчерпании —
   `UserActionException` с внятным текстом вместо 500; чистка staging в `Handle`
   обёрнута от маскировки ошибки. Покрытие: `PluginZipInstallerMoveTests`
   (лок спадает среди ретраев → перенос; постоянный лок → `UserActionException`),
   `UploadPluginTests`/`GetPluginTests` 6/6 без регрессии.

Проверено: проект `Mars.Plugin.Sdk` и все плагинные проекты собираются (полная
`Mars.slnx` в Release — 0 ошибок); `Mars.Plugin.Tests` 29/29 (+1 скип);
`Mars.Plugin.Integration.Tests` 9/9 (включая новые `PluginEndpointTests`);
`FileListUtilityTests` + `DirReadNodeTests` 7/7;
`dotnet pack` SDK даёт корректный
nupkg (`build/` таргеты без debug-манифеста, `mars/` оба манифеста релиза, `tools/`).
Контрольный `pack zip`/`pack nuget` на `MyMarsPlugin` (шаблон вне этого репо) —
на стороне плагина: в пакет не должен попадать `_front_plugins.json`, свои пакеты
автора бандлятся, марсовые стрипаются по замыканию.

Примечание: полная сборка `Mars.slnx` в Release падала на 5 ошибках в тестовых проектах
(`Mars.Datasource.Integration.Tests` — `configs`, `Mars.WebApiClient.Integration.Tests` —
`DummyAct`); причина — тесты ссылались на Debug-типы/поля (`DummyAct`, `configs`),
объявленные под `#if DEBUG`, вне дефайна. Починено 2026-09-01 оборачиванием этих
тест-методов в `#if DEBUG` (конвенция уже была для `FormTestAct`); Release-сборка 0 ошибок.

Дальше — Фазы 4–5 `PluginCatalogPlan.md` (установка по NuGet-id с проверкой статуса
в каталоге, витрина маркетплейса): этот реворк для них фундамент, пересмотра не нужно.
Фундамент готов: `packageType=MarsPlugin` + `mars-plugin.json` в пакетах,
`NuGetPluginInstaller` с настраиваемыми источниками, `InstallFromNuget` по id,
реестр/источники/жизненный цикл в админке.

### Фаза 8. Удаление и обновление плагинов без файлового лока — отложенные отметки в реестре — **выполнена 2026-09-01**

Проблема (2026-09-01): `PluginService.Uninstall` удаляет папку плагина немедленно,
но его entry-dll загружена в **неколлектируемый** ALC (`PluginLoadContext` без
`isCollectible`) — сборка является memory-mapped файлом и удерживается процессом
до рестарта. На Windows `Directory.Delete` бросает `IOException`; на Linux/Docker
unlink открытого файла разрешён, поэтому там удаление проходит. Хот-выгрузка
не вариант: типы плагина удерживают DI-регистрации (`ConfigureWebApplicationBuilder`),
эндпоинты пайплайна и миграции; модель жизненного цикла уже «изменения после рестарта».

Побочные проблемы той же причины (фиксятся здесь же):
- переустановка/обновление падает так же — `ZipInstaller`/`NugetInstaller` делают
  `DeleteDirectory(finalDir)` по папке с залоченной старой dll;
- отключённые плагины всё равно загружаются — проверка `Disabled` в реестре идёт
  после `InstantiatePlugin` (лок держится зря);
- после рестарта отключённый плагин исчезает из списка админки (список строится
  только из загруженных) и его нельзя включить обратно.

Решение (по решению пользователя 2026-09-01) — **отложенные отметки в реестре**
плагинов (`data/plugins/.registry.json` — единственный источник состояния,
никакого отдельного файла операций). Удаление всегда отложенное и единообразное
на всех платформах: при удалении ничего на диске не трогается, только отметка.

1. `PluginRegistryEntry` получает `PendingDelete: bool` и
   `PendingStagingDir: string?` (добавление полей обратно совместимо со
   старыми файлами реестра).
2. `PluginService.Uninstall`: вместо удаления папки и записи —
   `Registry.MarkPendingDelete(packageId)`. Контракт эндпоинта не меняется;
   админка уже показывает «Изменения применятся после рестарта».
3. Применение при старте: в начале `ConfigureBuilder`, **строго до
   `ReadPluginsFromDirectory`** (иначе папка, оставшаяся на диске, загрузится
   заново):
   - записи с `PendingStagingDir`: удалить старую папку, переименовать
     стейджинг → `<PackageId>`; успех — снять отметку; неудача — оставить
     до следующего рестарта + предупреждение в лог, плагин в этот старт не грузится;
   - записи с `PendingDelete`: удалить папку; успех — удалить запись из реестра;
     неудача — оставить отметку + предупреждение в лог;
   - страховка загрузчика: плагин с отметкой `PendingDelete` не грузится, даже
     если папку удалить не удалось.
4. Обновление в обоих инсталлерах: если старая папка удалилась (типично на
   Linux) — подмена сразу, как сейчас; если занята залоченной сборкой —
   стейджинг переименовывается в `plugins/_pending_<PackageId>_<guid>` и пишется
   `MarkInstalled(..., PendingStagingDir)`: новая версия видна в реестре сразу,
   файлы подмениваются при рестарте. `MarkInstalled` сбрасывает
   `PendingDelete`/`PendingStagingDir`, поэтому переустановка плагина,
   отмеченного к удалению, естественно отменяет удаление. Общая логика
   «подмени сейчас или отложи» — один хелпер для обоих инсталлеров.
5. Не грузить отключённые плагины: `ReadPluginsFromDirectory` проверяет
   `IsDisabled` по `PackageId` дескриптора до `InstantiatePlugin`; для legacy-папок
   без дескриптора остаётся нынешняя проверка после загрузки.
6. Отключённые и отмеченные к удалению плагины видны в списке админки: список
   становится объединением загруженных плагинов и записей реестра (только-реестровые —
   карточка по данным реестра: PackageId как заголовок, версия, источник, дата,
   статус «отключён»/«будет удалён после рестарта»). `SetEnabled`/`Uninstall`
   ищут плагин и в реестре тоже, чтобы отключённый можно было включить или удалить
   без загрузки. В `PluginInfoDto`/`PluginInfoResponse` — поле `PendingDelete`
   (+ бейдж в админке).

Вне рамок фазы:
- collectible ALC + `Unload()` — нежизнеспособно при текущих интеграциях плагина
  (см. выше); настоящая горячая выгрузка — отдельный большой проект;
- отмена удаления (снятие `PendingDelete`) — тривиальна на этой механике,
  добавить при желании отдельным пунктом.

Тесты (точечно):
- `Mars.Plugin.Tests`: реестр — `MarkPendingDelete`, `MarkInstalled` с
  `PendingStagingDir`, сброс отметок при переустановке, обратная совместимость
  со старым форматом файла; применение отметок на временных папках (симуляция
  лока файлом с атрибутом read-only → отметка остаётся; после снятия атрибута —
  довыполняется).
- `Mars.Integration.Tests` (`PluginLifecycleTests`): Uninstall плагина с реальной
  папкой → 200, запись отмечена `PendingDelete`, папка на месте; отключённый
  плагин виден в списке и включается обратно.
- Сборка `dotnet build Mars.slnx`.

Ручная проверка (Windows): установить плагин (zip и nuget) → удалить → рестарт →
папки нет; обновить работающий плагин → рестарт → в папке новая версия;
отключить → рестарт → плагин виден в списке как отключённый → включить → работает.

Документация: абзац в `ai/PluginCreationGuide.md` (Install & Lifecycle) — удаление
и обновление применяются при рестарте, файлы на диске до этого не трогаются.

Реализация и проверка (2026-09-01): по пунктам 1–6 без отклонений. Детали:
- `PluginRegistry`: `MarkPendingDelete`, `ClearPendingMarks`, `Entries`,
  `MarkInstalled(..., pendingStagingDir)` (сбрасывает отметки, сохраняет `Disabled`).
- `PluginManager.ApplyPendingOperations()` — в начале `ConfigureBuilder`,
  при пропавшем стейджинге отметка снимается с ошибкой в лог (нет бесконечного
  ретрая). В тестовом окружении (`EnvironmentName == "Test"`) НЕ вызывается:
  тестовый хост делит `data` с dev-инстансом (реестр у `PluginManager` дисковый
  даже в тестах — `PluginManager` создаётся в `AddPlugins` до `ConfigureTestServices`).
- `PluginInstallFinalizer.FinalizeAsync` — общий финал обоих инсталлеров
  (колбэк переноса: в zip-инсталлере — с ретраями `MoveInstalledPluginAsync`).
- `PluginService`: `Uninstall` только помечает; `SetEnabled`/`Uninstall` находят
  плагин и в реестре; список = загруженные ∪ только-реестровые.
- Админка: бейдж «Will be removed on restart», Disable/Delete для отмеченных
  заблокированы (Update — доступен и отменяет удаление).
- Попутно в тестах: убрано загрязнение общего реестра (клининг за инсталл-тестами,
  `InternalsVisibleTo` для `Mars.WebApiClient.Integration.Tests`), хрупкий
  ассерт «ровно 1 в списке» заменён на «содержит свой плагин».

Проверено: `dotnet build Mars.slnx` зелёная; `Mars.Plugin.Tests` 39/39 (+1 скип,
из них новых 10: реестр-отметки, применение при старте, симуляция лока,
детект с пропуском disabled/отмеченных); `Mars.Integration.Tests` (Plugins) 11/11
(новые: отложенное удаление с сохранением папки, отключённый в списке + включение);
`Mars.Plugin.Integration.Tests` 9/9; `Mars.WebApiClient.Integration.Tests` (Plugin) 4/4.
Ручная проверка на работающем инстансе (установка → удаление → рестарт) — за пользователем.

## Риски и ограничения

- **Переименование `WebApplicationPlugin` → `MarsPlugin`**: внешние плагины на старых
  именах не загрузятся (шимы удалены по решению пользователя 2026-09-01); все свои
  плагины и шаблоны переведены на новые имена в Фазе 1, внешние обновляются при выпуске.
- **Конфликт версий сторонних библиотек с хостом**: сборки из замыкания Mars всегда
  резолвятся из хоста (тип-идентичность важнее); плагин, требующий более новую версию
  «марсовой» библиотеки, должен ждать релиза Mars. Документируется как ограничение.
  До Фазы 4 тот же конфликт возможен и между двумя плагинами (дефолтный контекст).
- **Рестарт обязателен** для применения установки/обновления (загрузка при старте);
  горячая догрузка — отдельная большая тема, вне этого плана.
- **Безопасность**: плагин .NET — произвольный код без изоляции (ALC — не
  security-граница). Установка только `Admin`/`Developer`, blocklist, позже —
  подписанные пакеты и модерация каталога.
- **Тирсквоттинг** на nuget.org: организационно зарезервировать префикс `Mars.*` /
  `mdimai666.*` (задача вне кода, Фаза 0 `PluginDistributionPlan`).
